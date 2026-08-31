using System.Net;
using System.Text;

namespace SyasaiHidariCamera.Services;

public class HttpRequestService
{
	// private readonly Logger logger;
	private readonly HttpClient httpClient;

	/// <summary>
	/// HTTP要求サービスのインスタンスを生成し、初期化します。
	/// </summary>
	public HttpRequestService()
	{
		HttpClientHandler handler = new HttpClientHandler();
		handler.AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip;
		// ポート節約
		handler.MaxConnectionsPerServer = 10;
		this.httpClient = new HttpClient(handler)
		{
			DefaultRequestVersion = HttpVersion.Version20,
			DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower
		};
		// タイムアウトは1分とする
		this.httpClient.Timeout = TimeSpan.FromSeconds(60.0);
	}

	/// <summary>
	/// HTTP要求を送信します。
	/// </summary>
	/// <param name="httpMethod">HTTPメソッド</param>
	/// <param name="url">送信先URL</param>
	/// <param name="body">要求ボディー</param>
	/// <param name="cancellationToken">非同期処理中止トークン</param>
	/// <returns>HTTP応答メッセージ</returns>
	public async Task<HttpResponseMessage> HttpAsync(HttpMethod httpMethod, string url, string body, CancellationToken cancellationToken)
	{
		HttpRequestMessage httpRequestMessage = new HttpRequestMessage(httpMethod, url)
		{
			Content = new StringContent(body, new UTF8Encoding(false), "text/plain")
		};
		httpRequestMessage.Headers.Add("User-Agent", "Mozilla/5.0 BoyomiProxy/1.0");
		return await this.httpClient.SendAsync(httpRequestMessage, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
	}

}
