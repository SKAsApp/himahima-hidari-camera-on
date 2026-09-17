// Copilot作成
using Microsoft.AspNetCore.Mvc;
using SyasaiHidariCamera.Models.Api;
using SyasaiHidariCamera.Services;

namespace SyasaiHidariCamera.Controllers;

/// <summary>
/// ヘルスチェックを提供します。
/// </summary>
[ApiController]
public sealed class HealthController : ControllerBase
{

	/// <summary>versionServiceの値</summary>
	private readonly ApplicationVersionService versionService;

	/// <summary>
	/// コントローラーを初期化します。
	/// </summary>
	/// <param name="versionService">アプリケーションバージョンを提供するサービス</param>
	public HealthController(ApplicationVersionService versionService)
	{
		this.versionService = versionService;
	}

	/// <summary>
	/// 稼働状態とバージョンを返します。
	/// </summary>
	/// <returns>HTTP応答</returns>
	[HttpGet("/health")]
	public ActionResult<HealthResponse> Get( )
	{
		return this.Ok(new HealthResponse
		{
			Version = this.versionService.GetVersion( )
		});
	}
}
