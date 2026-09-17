// Copilot作成
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Serilog;
using SyasaiHidariCamera.Model.Api;
using SyasaiHidariCamera.Model.Setting;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// キュー内のコメントイベントを左カメラAPIへ送信します。
/// </summary>
public sealed class HidariCameraSenderService : BackgroundService
{
	/// <summary>名前付きHTTPクライアント生成元</summary>
	private readonly IHttpClientFactory httpClientFactory;

	/// <summary>左カメラAPI送信キュー</summary>
	private readonly HidariCameraQueue queue;

	/// <summary>左カメラAPI接続設定</summary>
	private readonly HidariCameraApiSettings settings;

	/// <summary>固定トークン</summary>
	private readonly string token;

	/// <summary>
	/// 左カメラAPI送信サービスを初期化します。
	/// </summary>
	/// <param name="httpClientFactory">HTTPクライアント生成元</param>
	/// <param name="queue">左カメラAPI送信キュー</param>
	/// <param name="settings">左カメラAPI接続設定</param>
	/// <param name="token">固定トークン</param>
	public HidariCameraSenderService(IHttpClientFactory httpClientFactory, HidariCameraQueue queue, HidariCameraApiSettings settings, string token)
	{
		this.httpClientFactory = httpClientFactory;
		this.queue = queue;
		this.settings = settings;
		this.token = token;
	}

	/// <summary>
	/// キューから要求を取り出して順番に送信します。
	/// </summary>
	/// <param name="stoppingToken">サービス停止を通知するトークン</param>
	/// <returns>常駐送信処理</returns>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		await foreach (CameraEventRequest request in this.queue.ReadAllAsync(stoppingToken))
		{
			await this.SendAsync(request, stoppingToken);
		}
	}

	/// <summary>
	/// 左カメラAPIへ要求を送信します。
	/// </summary>
	/// <param name="request">左カメラAPI要求</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>送信処理</returns>
	private async Task SendAsync(CameraEventRequest request, CancellationToken cancellationToken)
	{
		try
		{
			HttpClient httpClient = this.httpClientFactory.CreateClient("HidariCameraApi");
			using HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Post, this.settings.Url);
			httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.token);
			httpRequest.Content = JsonContent.Create(request);
			using HttpResponseMessage response = await httpClient.SendAsync(httpRequest, cancellationToken);
			string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			Log.Information("左カメラAPI送信が完了しました。要求識別子：{RequestId}　状態コード：{StatusCode}　応答：{ResponseBody}", request.RequestId, (int)response.StatusCode, responseBody);
		}
		catch (Exception exception)
		{
			Log.Warning(exception, "左カメラAPI送信中にエラーが発生しました。要求識別子：{RequestId}　詳細：{Exception}", request.RequestId, exception.ToString( ));
		}
	}
}
