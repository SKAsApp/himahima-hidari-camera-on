// Copilot作成
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// OBSメッセージ包絡を表します。
/// </summary>
public sealed class ObsMessageEnvelope
{
	/// <summary>操作コード</summary>
	[JsonPropertyName("op")]
	public int OpCode { get; set; }

	/// <summary>データ</summary>
	[JsonPropertyName("d")]
	public JsonElement Data { get; set; }
}
