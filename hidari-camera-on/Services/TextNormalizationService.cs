// Copilot作成
using System.Text;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// 判定対象文字列を正規化します。
/// </summary>
public sealed class TextNormalizationService
{
	/// <summary>音声認識文字列から削除する文字の一覧</summary>
	private static readonly char[] SpeechRemovedCharacters = [' ', '　', '、', '。', '，', ',', '．', '.', '？', '?', '！', '!', '・'];

	/// <summary>
	/// コメント本文を正規化します。
	/// </summary>
	/// <param name="text">正規化または判定する文字列</param>
	/// <returns>処理結果の文字列</returns>
	public string NormalizeComment(string text)
	{
		return this.NormalizeCommon(text);
	}

	/// <summary>
	/// 音声認識本文を正規化します。
	/// </summary>
	/// <param name="text">正規化または判定する文字列</param>
	/// <returns>処理結果の文字列</returns>
	public string NormalizeSpeech(string text)
	{
		string normalized = this.NormalizeCommon(text);
		foreach (char removedCharacter in SpeechRemovedCharacters)
		{
			normalized = normalized.Replace(removedCharacter.ToString( ), string.Empty, StringComparison.Ordinal);
		}
		return normalized;
	}

	/// <summary>
	/// 共通の正規化を行います。
	/// </summary>
	/// <param name="text">正規化または判定する文字列</param>
	/// <returns>処理結果の文字列</returns>
	private string NormalizeCommon(string text)
	{
		return (text ?? string.Empty).Normalize(NormalizationForm.FormKC).ToUpperInvariant( ).Replace("ひだり", "左", StringComparison.Ordinal).Replace("オン", "ON", StringComparison.Ordinal);
	}
}
