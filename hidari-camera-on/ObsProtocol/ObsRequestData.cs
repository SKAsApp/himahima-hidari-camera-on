// Copilot作成
using System.Text.Json.Serialization;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// OBS要求データを表します。
/// </summary>
public sealed class ObsRequestData
{
	/// <summary>要求種別</summary>
	[JsonPropertyName("requestType")]
	public string RequestType { get; set; } = string.Empty;

	/// <summary>要求識別子</summary>
	[JsonPropertyName("requestId")]
	public string RequestId { get; set; } = string.Empty;

	/// <summary>要求データ</summary>
	[JsonPropertyName("requestData")]
	public object RequestData { get; set; } = new { };
}
