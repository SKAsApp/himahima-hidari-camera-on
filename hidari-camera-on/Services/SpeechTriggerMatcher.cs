// Copilot作成
namespace SyasaiHidariCamera.Services;

/// <summary>
/// 音声認識の発動条件を判定します。
/// </summary>
public sealed class SpeechTriggerMatcher
{
	/// <summary>カメラ表示を発動する標準キーワード</summary>
	private const string StandardKeyword = "左カメラON";

	/// <summary>
	/// 今回文字列または前回文字列との結合で一致するか判定します。
	/// </summary>
	/// <param name="previousText">前回受信した正規化済み文字列</param>
	/// <param name="currentText">今回受信した正規化済み文字列</param>
	/// <returns>条件に一致する場合はtrue、それ以外の場合はfalse</returns>
	public bool IsMatch(string previousText, string currentText)
	{
		if (currentText.Contains(StandardKeyword, StringComparison.Ordinal) || currentText.Contains("左カメラ音", StringComparison.Ordinal))
		{
			return true;
		}
		for (int splitPosition = 1; splitPosition < StandardKeyword.Length; splitPosition++)
		{
			string prefix = StandardKeyword[..splitPosition];
			string suffix = StandardKeyword[splitPosition..];
			if (previousText.EndsWith(prefix, StringComparison.Ordinal) && currentText.StartsWith(suffix, StringComparison.Ordinal))
			{
				return true;
			}
		}
		return false;
	}
}
