// Copilot作成
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Domain;
using SyasaiHidariCamera.Models.Settings;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// OBS WebSocket Version 5との基本通信を担当します。
/// </summary>
public sealed class ObsWebSocketClient
{
	/// <summary>サービスの動作設定</summary>
	private readonly ObsWebSocketSettings settings;

	/// <summary>読み込んだ秘密情報</summary>
	private readonly TokenStore tokenStore;

	/// <summary>OBS WebSocket認証文字列を生成するサービス</summary>
	private readonly ObsAuthenticationService authenticationService;

	/// <summary>WebSocket送信を直列化するセマフォ</summary>
	private readonly SemaphoreSlim sendSemaphore = new(1, 1);

	/// <summary>応答待ち要求の一覧</summary>
	private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> pendingRequests = new( );

	/// <summary>OBS StudioとのWebSocket接続</summary>
	private ClientWebSocket? webSocket;

	/// <summary>現在の接続状態</summary>
	public ObsConnectionState State { get; private set; } = ObsConnectionState.Disconnected;

	/// <summary>
	/// クライアントを初期化します。
	/// </summary>
	/// <param name="options">設定を提供するオプション</param>
	/// <param name="tokenStore">読み込んだ秘密情報</param>
	/// <param name="authenticationService">authenticationServiceの値</param>
	public ObsWebSocketClient(IOptions<ObsWebSocketSettings> options, TokenStore tokenStore, ObsAuthenticationService authenticationService)
	{
		this.settings = options.Value;
		this.tokenStore = tokenStore;
		this.authenticationService = authenticationService;
	}

	/// <summary>
	/// OBS Studioへ接続し、識別処理を行います。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	public async Task ConnectAsync(CancellationToken cancellationToken)
	{
		this.State = ObsConnectionState.Connecting;
		this.webSocket = new ClientWebSocket( );
		this.webSocket.Options.AddSubProtocol("obswebsocket.json");
		await this.webSocket.ConnectAsync(new Uri(this.settings.Uri), cancellationToken);
		this.State = ObsConnectionState.Identifying;
		JsonElement hello = await this.ReceiveSingleMessageAsync(cancellationToken);
		JsonElement helloData = hello.GetProperty("d");
		Dictionary<string, object> identifyData = new( )
		{
			["rpcVersion"] = 1,
			["eventSubscriptions"] = 128
		};
		if (helloData.TryGetProperty("authentication", out JsonElement authentication))
		{
			identifyData["authentication"] = this.authenticationService.CreateAuthentication(this.tokenStore.ObsPassword, authentication.GetProperty("salt").GetString( )!, authentication.GetProperty("challenge").GetString( )!);
		}
		await this.SendEnvelopeAsync(ObsOpCode.Identify, identifyData, cancellationToken);
		JsonElement identified = await this.ReceiveSingleMessageAsync(cancellationToken);
		if (identified.GetProperty("op").GetInt32( ) != (int)ObsOpCode.Identified)
		{
			throw new InvalidDataException("OBS Studioの識別完了応答が不正です。");
		}
		this.State = ObsConnectionState.ConnectedNotReady;
	}

	/// <summary>
	/// OBS要求を送信します。
	/// </summary>
	/// <param name="requestType">OBS要求の種別</param>
	/// <param name="requestData">OBS要求に含めるデータ</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>OBS Studioから受信したJSONデータ</returns>
	public async Task<JsonElement> SendRequestAsync(string requestType, object requestData, CancellationToken cancellationToken)
	{
		if (this.webSocket is null || this.webSocket.State != WebSocketState.Open)
		{
			throw new InvalidOperationException("OBS Studioへ接続されていません。");
		}
		string requestId = Guid.NewGuid( ).ToString( );
		TaskCompletionSource<JsonElement> completionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
		this.pendingRequests[requestId] = completionSource;
		await this.SendEnvelopeAsync(ObsOpCode.Request, new ObsRequestData
		{
			RequestType = requestType,
			RequestId = requestId,
			RequestData = requestData
		}, cancellationToken);
		using CancellationTokenSource timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
		timeoutSource.CancelAfter(TimeSpan.FromSeconds(this.settings.RequestTimeoutSeconds));
		try
		{
			return await completionSource.Task.WaitAsync(timeoutSource.Token);
		}
		finally
		{
			this.pendingRequests.TryRemove(requestId, out _);
		}
	}

	/// <summary>
	/// 受信ループを実行します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	public async Task RunReceiveLoopAsync(CancellationToken cancellationToken)
	{
		while (this.webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
		{
			JsonElement message = await this.ReceiveSingleMessageAsync(cancellationToken);
			if (message.GetProperty("op").GetInt32( ) != (int)ObsOpCode.RequestResponse)
			{
				continue;
			}
			JsonElement data = message.GetProperty("d");
			string requestId = data.GetProperty("requestId").GetString( ) ?? string.Empty;
			if (this.pendingRequests.TryRemove(requestId, out TaskCompletionSource<JsonElement>? completionSource))
			{
				completionSource.TrySetResult(data.Clone( ));
			}
		}
		this.State = ObsConnectionState.Disconnected;
	}

	/// <summary>
	/// メッセージを送信します。
	/// </summary>
	/// <param name="opCode">OBS WebSocket操作コード</param>
	/// <param name="data">送信するデータ</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	private async Task SendEnvelopeAsync(ObsOpCode opCode, object data, CancellationToken cancellationToken)
	{
		byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { op = (int)opCode, d = data });
		await this.sendSemaphore.WaitAsync(cancellationToken);
		try
		{
			await this.webSocket!.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
		}
		finally
		{
			this.sendSemaphore.Release( );
		}
	}

	/// <summary>
	/// 単一のJSONメッセージを受信します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>OBS Studioから受信したJSONデータ</returns>
	private async Task<JsonElement> ReceiveSingleMessageAsync(CancellationToken cancellationToken)
	{
		byte[] buffer = new byte[8192];
		using MemoryStream stream = new( );
		WebSocketReceiveResult result;
		do
		{
			result = await this.webSocket!.ReceiveAsync(buffer, cancellationToken);
			stream.Write(buffer, 0, result.Count);
		}
		while (!result.EndOfMessage);
		if (result.MessageType == WebSocketMessageType.Close)
		{
			throw new WebSocketException("OBS Studioとの接続が終了しました。");
		}
		return JsonDocument.Parse(stream.ToArray( )).RootElement.Clone( );
	}
}
