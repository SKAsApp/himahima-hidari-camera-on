// Copilot作成
using System.Security.Cryptography;
using System.Text;
using SyasaiHidariCamera.Services;

namespace SyasaiHidariCamera.Middleware;

/// <summary>
/// エンドポイント別のBearerトークン認証を行います。
/// </summary>
public sealed class BearerTokenAuthenticationMiddleware
{
	/// <summary>次に実行する要求処理</summary>
	private readonly RequestDelegate next;

	/// <summary>
	/// ミドルウェアを初期化します。
	/// </summary>
	/// <param name="next">次に実行する要求処理</param>
	public BearerTokenAuthenticationMiddleware(RequestDelegate next)
	{
		this.next = next;
	}

	/// <summary>
	/// 認証対象要求を検証します。
	/// </summary>
	/// <param name="context">HTTP要求と応答のコンテキスト</param>
	/// <param name="tokenStore">読み込んだ秘密情報</param>
	/// <returns>非同期処理を表すタスク</returns>
	public async Task InvokeAsync(HttpContext context, TokenStore tokenStore)
	{
		if (HttpMethods.IsOptions(context.Request.Method) || context.Request.Path == "/health")
		{
			await this.next(context);
			return;
		}
		string? expectedToken = context.Request.Path.StartsWithSegments("/api/v1/comments") ? tokenStore.CommentToken : context.Request.Path.StartsWithSegments("/api/v1/speech-recognition") ? tokenStore.SpeechToken : null;
		if (expectedToken is null)
		{
			await this.next(context);
			return;
		}
		string authorization = context.Request.Headers.Authorization.ToString( );
		if (!authorization.StartsWith("Bearer ", StringComparison.Ordinal))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}
		byte[] actualBytes = Encoding.UTF8.GetBytes(authorization[7..]);
		byte[] expectedBytes = Encoding.UTF8.GetBytes(expectedToken);
		if (actualBytes.Length != expectedBytes.Length || !CryptographicOperations.FixedTimeEquals(actualBytes, expectedBytes))
		{
			context.Response.StatusCode = StatusCodes.Status401Unauthorized;
			return;
		}
		await this.next(context);
	}
}
