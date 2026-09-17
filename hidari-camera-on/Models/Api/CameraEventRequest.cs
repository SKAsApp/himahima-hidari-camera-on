// Copilot作成
using System.ComponentModel.DataAnnotations;

namespace SyasaiHidariCamera.Models.Api;

/// <summary>
/// カメライベント要求を表します。
/// </summary>
public sealed class CameraEventRequest
{

	/// <summary>要求識別子</summary>
	[Required]
	[StringLength(36)]
	public string RequestId { get; set; } = string.Empty;

	/// <summary>送信元</summary>
	[Required]
	[StringLength(64)]
	public string Source { get; set; } = string.Empty;

	/// <summary>イベント種別</summary>
	[Required]
	[StringLength(64)]
	public string EventType { get; set; } = string.Empty;

	/// <summary>本文</summary>
	[Required]
	[StringLength(4096)]
	public string Text { get; set; } = string.Empty;

	/// <summary>受信日時文字列</summary>
	[Required]
	public string ReceivedAt { get; set; } = string.Empty;

	/// <summary>セッション識別子</summary>
	[StringLength(36)]
	public string? SessionId { get; set; }

	/// <summary>
	/// 要求値の追加検証を行います。
	/// </summary>
	/// <returns>条件に一致する場合はtrue、それ以外の場合はfalse</returns>
	public bool HasValidIdentifiers( )
	{
		return Guid.TryParse(this.RequestId, out _) && DateTimeOffset.TryParse(this.ReceivedAt, out _);
	}
}
