// Copilot作成
using SyasaiHidariCamera.Model.Api;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// 左カメラAPIへ送信する要求を生成します。
/// </summary>
public sealed class RequestFactory
{
	/// <summary>
	/// コメントイベント要求を生成します。
	/// </summary>
	/// <param name="requestId">要求識別子</param>
	/// <param name="text">コメント本文</param>
	/// <param name="receivedAt">受信日時</param>
	/// <returns>コメントイベント要求</returns>
	public CameraEventRequest CreateCommentRequest(string requestId, string text, DateTimeOffset receivedAt)
	{
		return new CameraEventRequest
		{
			RequestId = requestId,
			Source = "boyomi-proxy",
			EventType = "talk",
			Text = text,
			ReceivedAt = receivedAt.ToString("O"),
			SessionId = null
		};
	}
}
