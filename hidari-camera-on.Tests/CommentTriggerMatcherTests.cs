// Copilot作成
using SyasaiHidariCamera.Services;
using Xunit;

namespace SyasaiHidariCamera.Tests;

/// <summary>
/// CommentTriggerMatcherの完全一致判定を検証します。
/// </summary>
public sealed class CommentTriggerMatcherTests
{
	/// <summary>テスト対象の判定サービス</summary>
	private readonly CommentTriggerMatcher matcher = new( );

	/// <summary>
	/// 標準キーワードと完全一致する場合に発動することを検証します。
	/// </summary>
	[Fact]
	public void IsMatch_WhenTextExactlyMatches_ReturnsTrue( )
	{
		bool actual = this.matcher.IsMatch("左カメラON");
		Assert.True(actual);
	}

	/// <summary>
	/// 前後に文字がある場合は発動しないことを検証します。
	/// </summary>
	[Theory]
	[InlineData("左カメラONお願いします")]
	[InlineData("今から左カメラON")]
	[InlineData("左カメラ ON")]
	public void IsMatch_WhenTextDoesNotExactlyMatch_ReturnsFalse(string text)
	{
		bool actual = this.matcher.IsMatch(text);
		Assert.False(actual);
	}
}
