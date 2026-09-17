using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using System.Net;
using Microsoft.AspNetCore.RateLimiting;
using Serilog;
using SyasaiHidariCamera.Common;
using SyasaiHidariCamera.Model.Setting;
using SyasaiHidariCamera.Services;


// 一旦強制的に日本設定にしている。
CultureInfo.CurrentCulture = new CultureInfo("ja-JP", false);
CultureInfo.CurrentUICulture = new CultureInfo("ja-JP", false);

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
// 設定
BoyomiProxySettings applicationSettings = new BoyomiProxySettings( );
builder.Configuration.Bind(applicationSettings);
ValidateSettings(applicationSettings);
// トークン
TokenFileService tokenFileService = new TokenFileService( );
string commentApiToken = tokenFileService.ReadRequiredToken(applicationSettings.HidariCameraApi.TokenFile);

// ロガーを
string executionId = Guid.NewGuid( ).ToString("D");
string logPath = Path.Combine(AppContext.BaseDirectory, "log", "boyomi-proxy-.log");
Log.Logger = new LoggerConfiguration( )
	.MinimumLevel.Information( )
	.Enrich.FromLogContext( )
	.Enrich.WithProperty("ExecutionId", executionId)
	.WriteTo.Console( )
	.WriteTo.File(logPath, rollingInterval: RollingInterval.Day, retainedFileCountLimit: applicationSettings.Logging.RetainedDays)
	.CreateLogger( );
builder.Host.UseSerilog( );
// シングルトンサービス
builder.Services.AddSingleton(applicationSettings.Boyomi);
builder.Services.AddSingleton(applicationSettings.HidariCameraApi);
builder.Services.AddSingleton(applicationSettings.RateLimit);
builder.Services.AddSingleton<TokenFileService>(tokenFileService);
builder.Services.AddSingleton(commentApiToken);
builder.Services.AddSingleton<HidariCameraQueue>( );
builder.Services.AddSingleton<RequestFactory>( );
builder.Services.AddSingleton<BoyomiClient>( );

builder.Services.AddHostedService<HidariCameraSenderService>( );
// JSON
builder.Services.AddControllers( )
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
		options.JsonSerializerOptions.WriteIndented = true;
		options.JsonSerializerOptions.IndentCharacter = '\t';
	});
// HTTPクライアント
builder.Services.AddHttpClient("Boyomi", httpClient =>
{
	httpClient.Timeout = TimeSpan.FromSeconds(applicationSettings.Boyomi.TimeoutSeconds);
	httpClient.DefaultRequestVersion = HttpVersion.Version20;
	httpClient.DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower;
})
.ConfigurePrimaryHttpMessageHandler(( ) => new HttpClientHandler
{
	AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip,
	MaxConnectionsPerServer = 10
});

builder.Services.AddHttpClient("HidariCameraApi", httpClient =>
{
	httpClient.Timeout = TimeSpan.FromSeconds(applicationSettings.HidariCameraApi.RequestTimeoutSeconds);
})
.ConfigurePrimaryHttpMessageHandler(( ) => new SocketsHttpHandler
{
	ConnectTimeout = TimeSpan.FromSeconds(applicationSettings.HidariCameraApi.ConnectTimeoutSeconds),
	AutomaticDecompression = DecompressionMethods.Brotli | DecompressionMethods.GZip
});

// API有効化
builder.Services.AddControllers( );
builder.Services.AddEndpointsApiExplorer( );

// レート制限：1秒に20リクエストのみ許可（それ以上はキューに入れて、キューの上限は360）
builder.Services.AddRateLimiter(options =>
{
	options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
	options.AddFixedWindowLimiter("ApiPolicy", limiterOptions =>
	{
		limiterOptions.PermitLimit = applicationSettings.RateLimit.PermitLimit;
		limiterOptions.Window = TimeSpan.FromSeconds(applicationSettings.RateLimit.WindowSeconds);
		limiterOptions.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
		limiterOptions.QueueLimit = applicationSettings.RateLimit.QueueLimit;
	});
});
// 待ち受け
builder.WebHost.UseUrls("http://" + applicationSettings.Server.ListenHost + ":" + applicationSettings.Server.ListenPort.ToString( ));
// アプリ
WebApplication app = builder.Build( );
app.UseSerilogRequestLogging( );
app.UseRateLimiter( );
app.MapGet("/", () => "棒読みちゃんプロキシーAPI")
	.RequireRateLimiting("ApiPolicy");;
app.MapControllers( )
	.RequireRateLimiting("ApiPolicy");

// 起動
try
{
	await app.RunAsync( );
}
finally
{
	await Log.CloseAndFlushAsync( );
}


/// <summary>
/// 起動に必要な設定値を検証します。
/// </summary>
/// <param name="settings">アプリケーション設定</param>
static void ValidateSettings(BoyomiProxySettings settings)
{
	if (settings.Server.ListenPort is < 1 or > 65535 || settings.Boyomi.Port is < 1 or > 65535)
	{
		throw new InvalidOperationException("ポート番号は1から65535の範囲で指定してください。");
	}
	if (!Uri.TryCreate(settings.HidariCameraApi.Url, UriKind.Absolute, out _))
	{
		throw new InvalidOperationException("左カメラAPIのURLが不正です。");
	}
	if (settings.HidariCameraApi.QueueCapacity < 1 || settings.RateLimit.PermitLimit < 1 || settings.RateLimit.WindowSeconds < 1)
	{
		throw new InvalidOperationException("キュー容量とレート制限値は1以上で指定してください。");
	}
}
