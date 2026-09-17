// Copilot作成
using SyasaiHidariCamera.Services;
using Xunit;

namespace SyasaiHidariCamera.Tests;

/// <summary>
/// TextNormalizationServiceの文字列正規化を検証します。
/// </summary>
public sealed class TextNormalizationServiceTests
{
	/// <summary>テスト対象のサービス</summary>
	private readonly TextNormalizationService service = new( );

	/// <summary>
	/// コメントの表記揺れを標準表記へ変換することを検証します。
	/// </summary>
	[Fact]
	public void NormalizeComment_WhenHiraganaAndKatakanaAreUsed_ReturnsStandardKeyword( )
	{
		string actual = this.service.NormalizeComment("ひだりカメラオン");
		Assert.Equal("左カメラON", actual);
	}

	/// <summary>
	/// コメントでは空白を削除しないことを検証します。
	/// </summary>
	[Fact]
	public void NormalizeComment_WhenTextContainsSpace_KeepsSpace( )
	{
		string actual = this.service.NormalizeComment("左カメラ ON");
		Assert.Equal("左カメラ ON", actual);
	}

	/// <summary>
	/// 音声認識では空白と句読点を削除することを検証します。
	/// </summary>
	[Fact]
	public void NormalizeSpeech_WhenTextContainsSpacesAndPunctuation_RemovesThem( )
	{
		string actual = this.service.NormalizeSpeech("左 カメラ、オン！");
		Assert.Equal("左カメラON", actual);
	}

	/// <summary>
	/// 全角英字を半角大文字へ変換することを検証します。
	/// </summary>
	[Fact]
	public void NormalizeSpeech_WhenFullWidthEnglishIsUsed_ReturnsHalfWidthUppercase( )
	{
		string actual = this.service.NormalizeSpeech("左カメラｏｎ");
		Assert.Equal("左カメラON", actual);
	}
}
