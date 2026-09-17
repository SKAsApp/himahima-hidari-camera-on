// Copilot作成
namespace SyasaiHidariCamera.Models.Settings;

/// <summary>
/// カメラ設定を表します。
/// </summary>
public sealed class CameraSettings
{
	/// <summary>表示秒数</summary>
	public int DisplaySeconds { get; set; } = 30;

	/// <summary>有効化再試行回数</summary>
	public int EnableRetryCount { get; set; } = 3;

	/// <summary>有効化再試行間隔</summary>
	public int EnableRetryIntervalMilliseconds { get; set; } = 1000;

	/// <summary>無効化再試行回数</summary>
	public int DisableRetryCount { get; set; } = 3;

	/// <summary>無効化再試行間隔</summary>
	public int DisableRetryIntervalMilliseconds { get; set; } = 1000;

	/// <summary>音声セッション有効期間</summary>
	public int SpeechSessionExpiryMinutes { get; set; } = 30;

	/// <summary>音声セッション最大数</summary>
	public int MaximumSpeechSessions { get; set; } = 10;
}
