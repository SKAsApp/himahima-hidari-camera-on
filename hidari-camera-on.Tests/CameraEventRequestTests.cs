// Copilot作成
using SyasaiHidariCamera.Models.Api;
using Xunit;

namespace SyasaiHidariCamera.Tests;

/// <summary>
/// CameraEventRequestの識別子と日時の検証を確認します。
/// </summary>
public sealed class CameraEventRequestTests
{
	/// <summary>
	/// 要求識別子と日時が正しい場合に有効と判定することを検証します。
	/// </summary>
	[Fact]
	public void HasValidIdentifiers_WhenValuesAreValid_ReturnsTrue( )
	{
		CameraEventRequest request = this.CreateRequest(Guid.NewGuid( ).ToString( ), "2026-09-12T20:00:00+09:00");
		Assert.True(request.HasValidIdentifiers( ));
	}

	/// <summary>
	/// 要求識別子または日時が不正な場合に無効と判定することを検証します。
	/// </summary>
	[Theory]
	[InlineData("invalid", "2026-09-12T20:00:00+09:00")]
	[InlineData("11111111-1111-4111-8111-111111111111", "invalid")]
	public void HasValidIdentifiers_WhenAnyValueIsInvalid_ReturnsFalse(string requestId, string receivedAt)
	{
		CameraEventRequest request = this.CreateRequest(requestId, receivedAt);
		Assert.False(request.HasValidIdentifiers( ));
	}

	/// <summary>
	/// 検証対象の要求を生成します。
	/// </summary>
	/// <param name="requestId">要求識別子</param>
	/// <param name="receivedAt">受信日時</param>
	/// <returns>生成した要求</returns>
	private CameraEventRequest CreateRequest(string requestId, string receivedAt)
	{
		return new CameraEventRequest
		{
			RequestId = requestId,
			Source = "boyomi-proxy",
			EventType = "comment",
			Text = "左カメラON",
			ReceivedAt = receivedAt
		};
	}
}
