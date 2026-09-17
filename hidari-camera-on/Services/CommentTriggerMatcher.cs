// Copilot作成
namespace SyasaiHidariCamera.Services;

/// <summary>
/// コメントの発動条件を判定します。
/// </summary>
public sealed class CommentTriggerMatcher
{
	/// <summary>
	/// 正規化済み本文が発動条件に一致するか判定します。
	/// </summary>
	/// <param name="normalizedText">正規化済みの文字列</param>
	/// <returns>条件に一致する場合はtrue、それ以外の場合はfalse</returns>
	public bool IsMatch(string normalizedText)
	{
		return string.Equals(normalizedText, "左カメラON", StringComparison.Ordinal);
	}
}
