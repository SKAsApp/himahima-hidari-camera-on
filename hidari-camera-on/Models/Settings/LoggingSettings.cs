// Copilot作成
namespace SyasaiHidariCamera.Models.Settings;

/// <summary>
/// ログ設定を表します。
/// </summary>
public sealed class LoggingSettings
{
	/// <summary>保持日数</summary>
	public int RetainedDays { get; set; } = 31;
}
