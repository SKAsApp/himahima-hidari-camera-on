using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;
using SyasaiHidariCamera.Services;

namespace SyasaiHidariCamera.Controllers;

/// <summary>
/// 棒読みちゃんへのリクエストをプロキシーし、読み上げを一時停止するコントローラー
/// </summary>
[ApiController]
[Route("/pause")]
public class BoyomiProxyPauseController : ControllerBase
{
	/// <summary>棒読みちゃんクライアント</summary>
	private readonly BoyomiClient boyomiClient;

	/// <summary>
	/// 一時停止要求コントローラーを初期化します。
	/// </summary>
	/// <param name="boyomiClient">棒読みちゃんクライアント</param>
	public BoyomiProxyPauseController(BoyomiClient boyomiClient)
	{
		this.boyomiClient = boyomiClient;
	}

	/// <summary>
	/// 一時停止要求を受け付けます。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>棒読みちゃんの応答</returns>
	[HttpGet(Name = "BoyomiProxyPause")]
	public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
	{
		string requestId = Guid.NewGuid( ).ToString("D");
		using (LogContext.PushProperty("RequestId", requestId))
		{
			Log.Information("棒読みちゃんプロキシー　読み上げ一時停止");
			BoyomiResponse response = await this.boyomiClient.PauseAsync(cancellationToken);
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
