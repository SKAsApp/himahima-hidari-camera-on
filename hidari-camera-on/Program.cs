// Copilot作成
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Options;
using Serilog;
using SyasaiHidariCamera.Middleware;
using SyasaiHidariCamera.Models.Settings;
using SyasaiHidariCamera.Services;

CultureInfo.DefaultThreadCurrentCulture = CultureInfo.GetCultureInfo("ja-JP");
CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.GetCultureInfo("ja-JP");

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);
ServerSettings serverSettings = builder.Configuration.GetSection("Server").Get<ServerSettings>() ?? throw new InvalidOperationException("Server設定を読み込めません。");
LoggingSettings loggingSettings = builder.Configuration.GetSection("Logging").Get<LoggingSettings>() ?? new LoggingSettings( );

Log.Logger = new LoggerConfiguration( )
	.MinimumLevel.Information( )
	.Enrich.FromLogContext( )
	.Enrich.WithProperty("ApplicationRunId", Guid.NewGuid( ))
	.WriteTo.Console( )
	.WriteTo.File("log/hidari-camera-on-.log", rollingInterval: RollingInterval.Day, retainedFileCountLimit: loggingSettings.RetainedDays)
	.CreateLogger( );

try
{
	builder.Host.UseSerilog( );
	builder.WebHost.UseUrls($"http://{serverSettings.ListenHost}:{serverSettings.ListenPort}");
	builder.Services.Configure<ServerSettings>(builder.Configuration.GetSection("Server"));
	builder.Services.Configure<SecuritySettings>(builder.Configuration.GetSection("Security"));
	builder.Services.Configure<CameraSettings>(builder.Configuration.GetSection("Camera"));
	builder.Services.Configure<ObsWebSocketSettings>(builder.Configuration.GetSection("ObsWebSocket"));
	builder.Services.Configure<RateLimitSettings>(builder.Configuration.GetSection("RateLimit"));
	builder.Services.AddSingleton<TokenFileService>();
	builder.Services.AddSingleton<TokenStore>(serviceProvider => serviceProvider.GetRequiredService<TokenFileService>().LoadRequiredTokens( ));
	builder.Services.AddSingleton<TextNormalizationService>();
	builder.Services.AddSingleton<CommentTriggerMatcher>();
	builder.Services.AddSingleton<SpeechTriggerMatcher>();
	builder.Services.AddSingleton<SpeechSessionStore>();
	builder.Services.AddSingleton<ObsAuthenticationService>();
	builder.Services.AddSingleton<ObsWebSocketClient>();
	builder.Services.AddSingleton<CameraTriggerService>();
	builder.Services.AddSingleton<ApplicationVersionService>();
	builder.Services.AddControllers( ).AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
		options.JsonSerializerOptions.WriteIndented = true;
		options.JsonSerializerOptions.IndentCharacter = '\t';
		options.JsonSerializerOptions.IndentSize = 1;
		options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
		options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
	});
	builder.Services.Configure<FormOptions>(options => options.MultipartBodyLengthLimit = serverSettings.MaximumRequestBodyBytes);
	SecuritySettings securitySettings = builder.Configuration.GetSection("Security").Get<SecuritySettings>() ?? throw new InvalidOperationException("Security設定を読み込めません。");
	builder.Services.AddCors(options => options.AddDefaultPolicy(policy => policy.WithOrigins(securitySettings.AllowedOrigins.ToArray( )).WithMethods("POST", "OPTIONS").WithHeaders("Authorization", "Content-Type").SetPreflightMaxAge(TimeSpan.FromHours(1))));
	RateLimitSettings rateLimitSettings = builder.Configuration.GetSection("RateLimit").Get<RateLimitSettings>() ?? new RateLimitSettings( );
	builder.Services.AddRateLimiter(options =>
	{
		options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
		options.AddPolicy("ApiPolicy", httpContext => RateLimitPartition.GetFixedWindowLimiter(httpContext.Connection.RemoteIpAddress?.ToString( ) ?? "unknown", _ => new FixedWindowRateLimiterOptions
		{
			PermitLimit = rateLimitSettings.PermitLimit,
			Window = TimeSpan.FromSeconds(rateLimitSettings.WindowSeconds),
			QueueLimit = 0,
			AutoReplenishment = true
		}));
	});

	WebApplication application = builder.Build( );
	application.Services.GetRequiredService<TokenStore>();
	application.Use(async (context, next) =>
	{
		context.Features.Get<IHttpMaxRequestBodySizeFeature>()!.MaxRequestBodySize = serverSettings.MaximumRequestBodyBytes;
		await next(context);
	});
	application.UseCors( );
	application.UseRateLimiter( );
	application.UseMiddleware<PrivateNetworkRestrictionMiddleware>();
	application.UseRouting( );
	application.UseMiddleware<BearerTokenAuthenticationMiddleware>();
	application.MapControllers( );
	application.Run( );
}
catch (Exception exception)
{
	Log.Fatal(exception, "アプリケーションの起動に失敗しました。　詳細：{Exception}", exception);
}
finally
{
	Log.CloseAndFlush( );
}
