using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using SyasaiHidariCamera.Services;
using SyasaiHidariCamera.Model.Setting;
using SyasaiHidariCamera.Common;

namespace SyasaiHidariCamera.Controllers;

/// <summary>
/// 棒読みちゃんへのリクエストをプロキシーし、条件に合致する文字列だったら処理を追加するコントローラー
/// </summary>
[ApiController]
[Route("/talk")]
public class BoyomiProxyController: ControllerBase
{
	private readonly IHLogger logger;

	public BoyomiProxyController(IHLogger logger)
	{
		this.logger = logger;
	}

	[HttpGet(Name = "BoyomiProxy")]
	public async Task<IActionResult> Get([FromQuery] string? text)
	{
		string requestId = Guid.NewGuid( ).ToString("D");
		this.logger.RequestId = requestId;
		this.logger.LogInformation("【起動】棒読みちゃんプロキシー開始　読み上げテキスト：" + text);
		GeneralSetting setting = Setting.GetInstance( ).SettingModel;
		CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
		CancellationToken cancellationToken = cancellationTokenSource.Token;
		string trueText = text ?? "";
		// 棒読みちゃんに転送する
		this.logger.LogDebug("棒読みちゃん転送：http://" + setting.BoyomiHost + ":" + setting.BoyomiPort.ToString( ) + "/talk?text=" + trueText);
		int boyomiStatusCode = 500;
		string boyomiResponseBody = "";
		try
		{
			HttpResponseMessage boyomiResponse = await new HttpRequestService().HttpAsync(HttpMethod.Get, "http://" + setting.BoyomiHost + ":" + setting.BoyomiPort.ToString( ) + "/talk?text=" + trueText, "", cancellationToken);
			boyomiStatusCode = (int) boyomiResponse.StatusCode;
			boyomiResponseBody = await boyomiResponse.Content.ReadAsStringAsync(cancellationToken);
		}
		catch (Exception e)
		{
			this.logger.LogException("棒読みちゃん転送中にエラーが発生しました。", e);
		}
		// 読み上げテキストに「左カメラON」が含まれていたら、OBSにキーを送信する
		if (trueText == "左カメラON" || trueText == "左カメラＯＮ" || trueText == "左カメラon" || trueText == "左カメラｏｎ")
		{
			this.logger.LogInformation("ホットキー条件「左カメラON」検出。OBSのシーンを変更します。");
			HotKeySendService hotKeySendService = new HotKeySendService(this.logger);
			this.logger.LogDebug("Control＋2送出");
			hotKeySendService.SendHotKey("obs64", "", 2);
			// 30秒後に戻すキーを送信する
			this.logger.LogDebug("30秒待機開始");
			await Task.Delay(30_000);
			this.logger.RequestId = requestId;
			this.logger.LogInformation("30秒待機終了。OBSのシーンを変更します。");
			this.logger.LogDebug("Control＋1送出");
			hotKeySendService.SendHotKey("obs64", "", 1);
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
