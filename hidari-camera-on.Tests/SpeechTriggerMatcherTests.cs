// Copilot作成
using SyasaiHidariCamera.Services;
using Xunit;

namespace SyasaiHidariCamera.Tests;

/// <summary>
/// SpeechTriggerMatcherの単一入力と分割入力の判定を検証します。
/// </summary>
public sealed class SpeechTriggerMatcherTests
{
	/// <summary>テスト対象の判定サービス</summary>
	private readonly SpeechTriggerMatcher matcher = new( );

	/// <summary>
	/// キーワードを含む単一入力で発動することを検証します。
	/// </summary>
	[Theory]
	[InlineData("左カメラON")]
	[InlineData("お願いします左カメラONです")]
	[InlineData("左カメラ音")]
	public void IsMatch_WhenCurrentTextContainsKeyword_ReturnsTrue(string currentText)
	{
		bool actual = this.matcher.IsMatch(string.Empty, currentText);
		Assert.True(actual);
	}

	/// <summary>
	/// 前回末尾と今回先頭を結合してキーワードになる場合に発動することを検証します。
	/// </summary>
	[Theory]
	[InlineData("左", "カメラON")]
	[InlineData("左カメラ", "ON")]
	[InlineData("左カメラO", "N")]
	public void IsMatch_WhenInputsFormSplitKeyword_ReturnsTrue(string previousText, string currentText)
	{
		bool actual = this.matcher.IsMatch(previousText, currentText);
		Assert.True(actual);
	}

	/// <summary>
	/// キーワードを構成しない場合に発動しないことを検証します。
	/// </summary>
	[Fact]
	public void IsMatch_WhenInputsDoNotFormKeyword_ReturnsFalse( )
	{
		bool actual = this.matcher.IsMatch("右カメラ", "OFF");
		Assert.False(actual);
	}
}
