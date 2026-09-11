// Copilot作成
namespace SyasaiHidariCamera.Models.Api;

/// <summary>
/// カメライベント応答を表します。
/// </summary>
public sealed class CameraEventResponse
{

	/// <summary>要求識別子</summary>
	public string RequestId { get; set; } = string.Empty;

	/// <summary>受付結果</summary>
	public bool Accepted { get; set; }

	/// <summary>発動結果</summary>
	public bool Triggered { get; set; }

	/// <summary>重複結果</summary>
	public bool Duplicate { get; set; }

	/// <summary>OBS操作結果</summary>
	public string ObsOperation { get; set; } = "notRequired";

	/// <summary>メッセージ</summary>
	public string Message { get; set; } = string.Empty;
}
