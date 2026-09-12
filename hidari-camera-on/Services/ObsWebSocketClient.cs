// Copilot作成
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Domain;
using SyasaiHidariCamera.Models.Settings;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// OBS WebSocket Version 5との通信を担当します。
/// </summary>
public sealed class ObsWebSocketClient : IAsyncDisposable
{
	/// <summary>OBS WebSocket設定</summary>
	private readonly ObsWebSocketSettings settings;

	/// <summary>読み込んだ秘密情報</summary>
	private readonly TokenStore tokenStore;

	/// <summary>OBS WebSocket認証文字列を生成するサービス</summary>
	private readonly ObsAuthenticationService authenticationService;

	/// <summary>JSONの読み書きに使用するオプション</summary>
	private readonly JsonSerializerOptions jsonSerializerOptions;

	/// <summary>WebSocket送信を直列化するセマフォ</summary>
	private readonly SemaphoreSlim sendSemaphore = new(1, 1);

	/// <summary>接続処理を直列化するセマフォ</summary>
	private readonly SemaphoreSlim connectionSemaphore = new(1, 1);

	/// <summary>応答待ち要求の一覧</summary>
	private readonly ConcurrentDictionary<string, TaskCompletionSource<JsonElement>> pendingRequests = new( );

	/// <summary>OBS StudioとのWebSocket接続</summary>
	private ClientWebSocket? webSocket;

	/// <summary>現在の接続状態</summary>
	public ObsConnectionState State { get; private set; } = ObsConnectionState.Disconnected;

	/// <summary>シーンアイテム表示状態の変更通知</summary>
	public event EventHandler<ObsSceneItemStateChangedEventArgs>? SceneItemStateChanged;

	/// <summary>
	/// クライアントを初期化します。
	/// </summary>
	/// <param name="options">OBS WebSocket設定を提供するオプション</param>
	/// <param name="tokenStore">読み込んだ秘密情報</param>
	/// <param name="authenticationService">OBS WebSocket認証文字列を生成するサービス</param>
	/// <param name="jsonOptions">JSONの読み書きに使用するオプション</param>
	public ObsWebSocketClient(IOptions<ObsWebSocketSettings> options, TokenStore tokenStore, ObsAuthenticationService authenticationService, IOptions<JsonOptions> jsonOptions)
	{
		this.settings = options.Value;
		this.tokenStore = tokenStore;
		this.authenticationService = authenticationService;
		this.jsonSerializerOptions = jsonOptions.Value.JsonSerializerOptions;
	}

	/// <summary>
	/// OBS Studioへ接続し、識別処理を行います。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	public async Task ConnectAsync(CancellationToken cancellationToken)
	{
		await this.connectionSemaphore.WaitAsync(cancellationToken);
		try
		{
			await this.DisconnectCoreAsync(CancellationToken.None);
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
		catch
		{
			this.State = ObsConnectionState.Disconnected;
			throw;
		}
		finally
		{
			this.connectionSemaphore.Release( );
		}
	}

	/// <summary>
	/// OBS要求を送信します。
	/// </summary>
	/// <param name="requestType">OBS要求の種別</param>
	/// <param name="requestData">OBS要求に含めるデータ</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>OBS Studioから受信した応答データ</returns>
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
			JsonElement responseData = await completionSource.Task.WaitAsync(timeoutSource.Token);
			JsonElement requestStatus = responseData.GetProperty("requestStatus");
			if (!requestStatus.GetProperty("result").GetBoolean( ))
			{
				int code = requestStatus.GetProperty("code").GetInt32( );
				string comment = requestStatus.TryGetProperty("comment", out JsonElement commentElement) ? commentElement.GetString( ) ?? string.Empty : string.Empty;
				throw new InvalidOperationException($"OBS Studio要求が失敗しました。コード：{code}、内容：{comment}");
			}
			return responseData.TryGetProperty("responseData", out JsonElement response) ? response.Clone( ) : default;
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
		try
		{
			while (this.webSocket?.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
			{
				JsonElement message = await this.ReceiveSingleMessageAsync(cancellationToken);
				this.ProcessReceivedMessage(message);
			}
		}
		finally
		{
			this.State = ObsConnectionState.Disconnected;
			this.CancelPendingRequests( );
		}
	}

	/// <summary>
	/// 接続状態を準備完了へ変更します。
	/// </summary>
	public void MarkReady( )
	{
		if (this.State == ObsConnectionState.ConnectedNotReady)
		{
			this.State = ObsConnectionState.Ready;
		}
	}

	/// <summary>
	/// OBS Studioとの接続を正常終了します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	public async Task DisconnectAsync(CancellationToken cancellationToken)
	{
		await this.connectionSemaphore.WaitAsync(cancellationToken);
		try
		{
			await this.DisconnectCoreAsync(cancellationToken);
		}
		finally
		{
			this.connectionSemaphore.Release( );
		}
	}

	/// <summary>
	/// 使用しているリソースを解放します。
	/// </summary>
	/// <returns>非同期処理を表すタスク</returns>
	public async ValueTask DisposeAsync( )
	{
		await this.DisconnectAsync(CancellationToken.None);
		this.sendSemaphore.Dispose( );
		this.connectionSemaphore.Dispose( );
	}

	/// <summary>
	/// 受信メッセージを種類別に処理します。
	/// </summary>
	/// <param name="message">受信したJSONメッセージ</param>
	private void ProcessReceivedMessage(JsonElement message)
	{
		int opCode = message.GetProperty("op").GetInt32( );
		JsonElement data = message.GetProperty("d");
		if (opCode == (int)ObsOpCode.RequestResponse)
		{
			string requestId = data.GetProperty("requestId").GetString( ) ?? string.Empty;
			if (this.pendingRequests.TryRemove(requestId, out TaskCompletionSource<JsonElement>? completionSource))
			{
				completionSource.TrySetResult(data.Clone( ));
			}
			return;
		}
		if (opCode != (int)ObsOpCode.Event)
		{
			return;
		}
		string eventType = data.GetProperty("eventType").GetString( ) ?? string.Empty;
		if (eventType != "SceneItemEnableStateChanged")
		{
			return;
		}
		JsonElement eventData = data.GetProperty("eventData");
		this.SceneItemStateChanged?.Invoke(this, new ObsSceneItemStateChangedEventArgs
		{
			SceneName = eventData.GetProperty("sceneName").GetString( ) ?? string.Empty,
			SceneItemId = eventData.GetProperty("sceneItemId").GetInt32( ),
			SceneItemEnabled = eventData.GetProperty("sceneItemEnabled").GetBoolean( )
		});
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
		byte[] bytes = JsonSerializer.SerializeToUtf8Bytes(new { op = (int)opCode, d = data }, this.jsonSerializerOptions);
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
	/// <returns>受信したJSONメッセージ</returns>
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

	/// <summary>
	/// 内部のWebSocket接続を終了します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	private async Task DisconnectCoreAsync(CancellationToken cancellationToken)
	{
		this.State = ObsConnectionState.Stopping;
		ClientWebSocket? currentWebSocket = this.webSocket;
		this.webSocket = null;
		if (currentWebSocket is not null && currentWebSocket.State == WebSocketState.Open)
		{
			await currentWebSocket.CloseOutputAsync(WebSocketCloseStatus.NormalClosure, "アプリケーションを終了します。", cancellationToken);
		}
		currentWebSocket?.Dispose( );
		this.CancelPendingRequests( );
		this.State = ObsConnectionState.Disconnected;
	}

	/// <summary>
	/// 応答待ち要求を取り消します。
	/// </summary>
	private void CancelPendingRequests( )
	{
		foreach (KeyValuePair<string, TaskCompletionSource<JsonElement>> pendingRequest in this.pendingRequests)
		{
			pendingRequest.Value.TrySetException(new InvalidOperationException("OBS Studioとの接続が終了しました。"));
		}
		this.pendingRequests.Clear( );
	}
}
