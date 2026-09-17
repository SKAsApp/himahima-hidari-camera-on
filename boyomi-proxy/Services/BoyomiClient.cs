// Copilot作成
using Microsoft.AspNetCore.WebUtilities;
using Serilog;
using SyasaiHidariCamera.Model.Setting;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// 棒読みちゃん互換APIへ要求を中継します。
/// </summary>
public sealed class BoyomiClient
{
	/// <summary>名前付きHTTPクライアント生成元</summary>
	private readonly IHttpClientFactory httpClientFactory;

	/// <summary>棒読みちゃん接続設定</summary>
	private readonly BoyomiSettings settings;

	/// <summary>
	/// 棒読みちゃんクライアントを初期化します。
	/// </summary>
	/// <param name="httpClientFactory">HTTPクライアント生成元</param>
	/// <param name="settings">棒読みちゃん接続設定</param>
	public BoyomiClient(IHttpClientFactory httpClientFactory, BoyomiSettings settings)
	{
		this.httpClientFactory = httpClientFactory;
		this.settings = settings;
	}

	/// <summary>
	/// 読み上げ要求を送信します。
	/// </summary>
	/// <param name="text">読み上げ本文</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>棒読みちゃんの応答情報</returns>
	public Task<BoyomiResponse> TalkAsync(string text, CancellationToken cancellationToken)
	{
		return this.SendAsync("/talk", new Dictionary<string, string?> { ["text"] = text }, cancellationToken);
	}

	/// <summary>
	/// 一時停止要求を送信します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>棒読みちゃんの応答情報</returns>
	public Task<BoyomiResponse> PauseAsync(CancellationToken cancellationToken)
	{
		return this.SendAsync("/pause", null, cancellationToken);
	}

	/// <summary>
	/// 再開要求を送信します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>棒読みちゃんの応答情報</returns>
	public Task<BoyomiResponse> ResumeAsync(CancellationToken cancellationToken)
	{
		return this.SendAsync("/resume", null, cancellationToken);
	}

	/// <summary>
	/// 棒読みちゃんへHTTP要求を送信します。
	/// </summary>
	/// <param name="path">要求パス</param>
	/// <param name="query">クエリーパラメーター</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>棒読みちゃんの応答情報</returns>
	private async Task<BoyomiResponse> SendAsync(string path, IDictionary<string, string?>? query, CancellationToken cancellationToken)
	{
		string baseUrl = "http://" + this.settings.Host + ":" + this.settings.Port.ToString( ) + path;
		string url = query is null ? baseUrl : QueryHelpers.AddQueryString(baseUrl, query);
		try
		{
			HttpClient httpClient = this.httpClientFactory.CreateClient("Boyomi");
			using HttpResponseMessage response = await httpClient.GetAsync(url, cancellationToken);
			string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
			return new BoyomiResponse((int)response.StatusCode, responseBody);
		}
		catch (Exception exception)
		{
			Log.Error(exception, "棒読みちゃん転送中にエラーが発生しました。　詳細：{Exception}", exception.ToString( ));
			return new BoyomiResponse(StatusCodes.Status500InternalServerError, string.Empty);
		}
	}
}

/// <summary>
/// 棒読みちゃんから受信した応答を表します。
/// </summary>
/// <param name="StatusCode">HTTPステータスコード</param>
/// <param name="Body">応答本文</param>
public sealed record BoyomiResponse(int StatusCode, string Body);
