// Copilot作成
using System.Security.Cryptography;
using System.Text;
using SyasaiHidariCamera.Services;
using Xunit;

namespace SyasaiHidariCamera.Tests;

/// <summary>
/// ObsAuthenticationServiceの認証文字列生成を検証します。
/// </summary>
public sealed class ObsAuthenticationServiceTests
{
	/// <summary>
	/// パスワード、ソルト、チャレンジから仕様どおりの認証文字列を生成することを検証します。
	/// </summary>
	[Fact]
	public void CreateAuthentication_WhenValuesAreProvided_ReturnsDoubleHashedBase64( )
	{
		ObsAuthenticationService service = new( );
		string password = "password";
		string salt = "salt";
		string challenge = "challenge";
		string secret = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
		string expected = Convert.ToBase64String(SHA256.HashData(Encoding.UTF8.GetBytes(secret + challenge)));
		string actual = service.CreateAuthentication(password, salt, challenge);
		Assert.Equal(expected, actual);
	}
}
