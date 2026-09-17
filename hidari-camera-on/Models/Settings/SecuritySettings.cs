// Copilot作成
namespace SyasaiHidariCamera.Models.Settings;

/// <summary>
/// セキュリティ設定を表します。
/// </summary>
public sealed class SecuritySettings
{
	/// <summary>許可ネットワーク</summary>
	public List<string> AllowedNetworks { get; set; } = [];

	/// <summary>許可オリジン</summary>
	public List<string> AllowedOrigins { get; set; } = [];

	/// <summary>コメント用トークンファイル</summary>
	public string CommentTokenFile { get; set; } = string.Empty;

	/// <summary>音声認識用トークンファイル</summary>
	public string SpeechTokenFile { get; set; } = string.Empty;
}
