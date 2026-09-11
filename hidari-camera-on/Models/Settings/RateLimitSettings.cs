// Copilot作成
namespace SyasaiHidariCamera.Models.Settings;

/// <summary>
/// レート制限設定を表します。
/// </summary>
public sealed class RateLimitSettings
{
	/// <summary>許可件数</summary>
	public int PermitLimit { get; set; } = 20;

	/// <summary>ウィンドウ秒数</summary>
	public int WindowSeconds { get; set; } = 1;
}
