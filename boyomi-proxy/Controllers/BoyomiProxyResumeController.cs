using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Context;
using SyasaiHidariCamera.Services;

namespace SyasaiHidariCamera.Controllers;

/// <summary>
/// 棒読みちゃんへのリクエストをプロキシーし、読み上げを再開するコントローラー
/// </summary>
[ApiController]
[Route("/resume")]
public class BoyomiProxyResumeController : ControllerBase
{
	/// <summary>棒読みちゃんクライアント</summary>
	private readonly BoyomiClient boyomiClient;

	/// <summary>
	/// 再開コントローラーを初期化します。
	/// </summary>
	/// <param name="boyomiClient">棒読みちゃんクライアント</param>
	public BoyomiProxyResumeController(BoyomiClient boyomiClient)
	{
		this.boyomiClient = boyomiClient;
	}

	/// <summary>
	/// 再開要求を受け付けます。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>棒読みちゃんの応答</returns>
	[HttpGet(Name = "BoyomiProxyResume")]
	public async Task<IActionResult> GetAsync(CancellationToken cancellationToken)
	{
		string requestId = Guid.NewGuid( ).ToString("D");
		using (LogContext.PushProperty("RequestId", requestId))
		{
			Log.Information("棒読みちゃんプロキシー　読み上げ再開");
			BoyomiResponse response = await this.boyomiClient.ResumeAsync(cancellationToken);
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
