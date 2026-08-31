using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Unicode;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using SyasaiHidariCamera.Common;
using SyasaiHidariCamera.Model.Setting;

// 一旦強制的に日本設定にしている。
CultureInfo.CurrentCulture = new CultureInfo("ja-JP", false);
Setting setting = Setting.GetInstance( );
int listenPort = setting.SettingModel.ListenPort;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

// 独自ロガーをDI登録
string executionId = Guid.NewGuid( ).ToString("D");
builder.Services.AddSingleton<IHLogger>(new HLogger(executionId, true));

// Add services to the container.
builder.Services.AddControllers( )
	.AddJsonOptions(options =>
	{
		options.JsonSerializerOptions.Encoder = JavaScriptEncoder.Create(UnicodeRanges.All);
		options.JsonSerializerOptions.WriteIndented = true;
		options.JsonSerializerOptions.IndentCharacter = '\t';
	});
builder.Services.AddEndpointsApiExplorer( );
builder.Services.AddMvc( ).AddWebApiConventions( );

// 1 msに1リクエストのみ許可（それ以上はキューに入れて、キューの上限は360）
builder.Services.AddRateLimiter(config =>
	config.AddFixedWindowLimiter("fixedLimit", options =>
	{
		options.PermitLimit = 1;
		options.Window = TimeSpan.FromMilliseconds(1.0);
		options.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
		options.QueueLimit = 360;
	})
);

WebApplication app = builder.Build();
app.Urls.Add("http://localhost:" + listenPort.ToString( ));
app.MapGet("/", () => "左カメラONにするやつAPI")
	.RequireRateLimiting("fixedLimit");
app.MapControllers()
	.RequireRateLimiting("fixedLimit");

app.Run( );
