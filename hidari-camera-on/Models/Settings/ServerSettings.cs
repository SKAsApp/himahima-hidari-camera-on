// Copilot作成
namespace SyasaiHidariCamera.Models.Settings;

/// <summary>
/// サーバー設定を表します。
/// </summary>
public sealed class ServerSettings
{
	/// <summary>待受ホスト</summary>
	public string ListenHost { get; set; } = "0.0.0.0";

	/// <summary>待受ポート</summary>
	public int ListenPort { get; set; } = 15082;

	/// <summary>要求本文の最大バイト数</summary>
	public long MaximumRequestBodyBytes { get; set; } = 65536;
}
