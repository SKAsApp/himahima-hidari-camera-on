// Copilot作成
namespace SyasaiHidariCamera.Models.Api;

/// <summary>
/// ヘルスチェック応答を表します。
/// </summary>
public sealed class HealthResponse
{

	/// <summary>稼働状態</summary>
	public string Status { get; set; } = "Healthy";

	/// <summary>バージョン</summary>
	public string Version { get; set; } = string.Empty;
}
