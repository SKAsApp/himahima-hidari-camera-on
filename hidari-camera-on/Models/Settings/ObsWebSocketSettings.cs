// Copilot作成
namespace SyasaiHidariCamera.Models.Settings;

/// <summary>
/// OBS WebSocket設定を表します。
/// </summary>
public sealed class ObsWebSocketSettings
{
	/// <summary>接続先</summary>
	public string Uri { get; set; } = "ws://127.0.0.1:4455";

	/// <summary>パスワードファイル</summary>
	public string PasswordFile { get; set; } = string.Empty;

	/// <summary>シーン名</summary>
	public string SceneName { get; set; } = string.Empty;

	/// <summary>ソース名</summary>
	public string SourceName { get; set; } = string.Empty;

	/// <summary>再接続間隔</summary>
	public int ReconnectIntervalSeconds { get; set; } = 15;

	/// <summary>要求タイムアウト</summary>
	public int RequestTimeoutSeconds { get; set; } = 5;
}
