using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;
using SyasaiHidariCamera.Services;

namespace SyasaiHidariCamera.Controllers;

/// <summary>
/// 棒読みちゃんへのリクエストをプロキシーし、hidari-camera-on（コメント処理API）にも送信する（キューに入れる）コントローラー
/// </summary>
[ApiController]
[Route("/talk")]
public class BoyomiProxyController: ControllerBase
{
	/// <summary>棒読みちゃんクライアント</summary>
	private readonly BoyomiClient boyomiClient;

	/// <summary>左カメラAPI送信キュー</summary>
	private readonly HidariCameraQueue hidariCameraQueue;

	/// <summary>要求生成サービス</summary>
	private readonly RequestFactory requestFactory;

	/// <summary>
	/// 読み上げ要求コントローラーを初期化します。
	/// </summary>
	/// <param name="boyomiClient">棒読みちゃんクライアント</param>
	/// <param name="hidariCameraQueue">左カメラAPI送信キュー</param>
	/// <param name="requestFactory">要求生成サービス</param>
	public BoyomiProxyController(BoyomiClient boyomiClient, HidariCameraQueue hidariCameraQueue, RequestFactory requestFactory)
	{
		this.boyomiClient = boyomiClient;
		this.hidariCameraQueue = hidariCameraQueue;
		this.requestFactory = requestFactory;
	}

	/// <summary>
	/// 読み上げ要求を受け付けます。
	/// </summary>
	/// <param name="text">読み上げ本文</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>棒読みちゃんの応答</returns>
	[HttpGet(Name = "BoyomiProxy")]
	public async Task<IActionResult> GetAsync([FromQuery] string? text, CancellationToken cancellationToken)
	{
		string requestId = Guid.NewGuid( ).ToString("D");
		DateTimeOffset receivedAt = DateTimeOffset.Now;
		string actualText = text ?? string.Empty;
		using (LogContext.PushProperty("RequestId", requestId))
		{
			Log.Information("棒読みちゃんプロキシー開始　読み上げ本文：{Text}", actualText);
			// 棒読みちゃんへの要求＆hidari-camera-onへの送信キュー追加
			Task<BoyomiResponse> boyomiTask = this.boyomiClient.TalkAsync(actualText, cancellationToken);
			this.hidariCameraQueue.TryEnqueue(this.requestFactory.CreateCommentRequest(requestId, actualText, receivedAt));
			BoyomiResponse response = await boyomiTask;
			Log.Debug("棒読みちゃん応答　状態コード：{StatusCode}、応答：{ResponseBody}", response.StatusCode, response.Body);
			// 応答
			ContentResult contentResult = new ContentResult( )
			{
				StatusCode = response.StatusCode,
				ContentType = "application/json; charset=UTF-8",
				Content = response.Body
			};
			return contentResult;
		}
	}
	
}
