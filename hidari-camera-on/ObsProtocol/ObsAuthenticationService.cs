// Copilot作成
using System.Security.Cryptography;
using System.Text;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// OBS WebSocket認証文字列を生成します。
/// </summary>
public sealed class ObsAuthenticationService
{
	/// <summary>
	/// 公式手順に従って認証文字列を生成します。
	/// </summary>
	/// <param name="password">OBS WebSocketパスワード</param>
	/// <param name="salt">認証用のソルト</param>
	/// <param name="challenge">認証用のチャレンジ</param>
	/// <returns>処理結果の文字列</returns>
	public string CreateAuthentication(string password, string salt, string challenge)
	{
		string secret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
		return Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge)));
	}
}
