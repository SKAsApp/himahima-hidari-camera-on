using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyasaiHidariCamera.Services;
using SyasaiHidariCamera.Model.Setting;
using SyasaiHidariCamera.Common;

namespace SyasaiHidariCamera.Controllers;

/// <summary>
/// 棒読みちゃんへのリクエストをプロキシーし、読み上げを再開するコントローラー
/// </summary>
[ApiController]
[Route("/resume")]
public class BoyomiProxyResumeController : ControllerBase
{
	private readonly IHLogger logger;

	public BoyomiProxyResumeController(IHLogger logger)
	{
		this.logger = logger;
	}

	[HttpGet(Name = "BoyomiProxyResume")]
	public async Task<IActionResult> Get( )
	{
		this.logger.RequestId = Guid.NewGuid( ).ToString("D");
		this.logger.LogInformation("【起動】棒読みちゃんプロキシー再開");
		GeneralSetting setting = Setting.GetInstance( ).SettingModel;
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource( );
		CancellationToken cancellationToken = cancellationTokenSource.Token;
		// 棒読みちゃんに転送する
		this.logger.LogDebug("棒読みちゃん転送：http://" + setting.BoyomiHost + ":" + setting.BoyomiPort.ToString( ) + "/resume");
		int boyomiStatusCode = 500;
		string boyomiResponseBody = "";
		try
		{
			HttpResponseMessage boyomiResponse = await new HttpRequestService( ).HttpAsync(HttpMethod.Get, "http://" + setting.BoyomiHost + ":" + setting.BoyomiPort.ToString( ) + "/resume", "", cancellationToken);
			boyomiStatusCode = (int) boyomiResponse.StatusCode;
			boyomiResponseBody = await boyomiResponse.Content.ReadAsStringAsync(cancellationToken);
		}
		catch (Exception e)
		{
			this.logger.LogException("棒読みちゃん転送中にエラーが発生しました。", e);
		}
		// 応答する
		ContentResult contentResult = new ContentResult()
		{
			StatusCode = boyomiStatusCode,
			ContentType = "application/json; charset=UTF-8",
			Content = boyomiResponseBody
		};
		this.logger.LogDebug("応答：" + boyomiStatusCode.ToString( ) + "　" + boyomiResponseBody);
		return contentResult;
	}
	
}
