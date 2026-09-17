// Copilot作成
using System.Net;
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Settings;

namespace SyasaiHidariCamera.Middleware;

/// <summary>
/// 接続元IPアドレスをプライベートネットワークへ制限します。
/// </summary>
public sealed class PrivateNetworkRestrictionMiddleware
{
	/// <summary>次に実行する要求処理</summary>
	private readonly RequestDelegate next;

	/// <summary>ログ出力を行うロガー</summary>
	private readonly ILogger<PrivateNetworkRestrictionMiddleware> logger;

	/// <summary>
	/// ミドルウェアを初期化します。
	/// </summary>
	/// <param name="next">次に実行する要求処理</param>
	/// <param name="logger">ログ出力を行うロガー</param>
	public PrivateNetworkRestrictionMiddleware(RequestDelegate next, ILogger<PrivateNetworkRestrictionMiddleware> logger)
	{
		this.next = next;
		this.logger = logger;
	}

	/// <summary>
	/// 許可IPアドレスだけを後続へ渡します。
	/// </summary>
	/// <param name="context">HTTP要求と応答のコンテキスト</param>
	/// <returns>非同期処理を表すタスク</returns>
	public async Task InvokeAsync(HttpContext context)
	{
		IPAddress? address = context.Connection.RemoteIpAddress;
		if (address?.IsIPv4MappedToIPv6 == true)
		{
			address = address.MapToIPv4( );
		}
		if (address is null || !IsAllowed(address))
		{
			this.logger.LogWarning("許可されていない接続元IPアドレスです。接続元：{RemoteIpAddress}", address);
			context.Response.StatusCode = StatusCodes.Status403Forbidden;
			return;
		}
		await this.next(context);
	}

	/// <summary>
	/// IPアドレスが許可範囲か判定します。
	/// </summary>
	/// <param name="address">addressの値</param>
	/// <returns>条件に一致する場合はtrue、それ以外の場合はfalse</returns>
	private static bool IsAllowed(IPAddress address)
	{
		if (IPAddress.IsLoopback(address))
		{
			return true;
		}
		if (address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
		{
			return false;
		}
		byte[] bytes = address.GetAddressBytes( );
		if (bytes[0] == 10)
		{
			return true;
		}
		if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
		{
			return true;
		}
		return bytes[0] == 192 && bytes[1] == 168;
	}
}
