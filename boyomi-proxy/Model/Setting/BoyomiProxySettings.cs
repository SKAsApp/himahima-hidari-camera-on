// Copilot作成
namespace SyasaiHidariCamera.Model.Setting;

/// <summary>
/// 棒読みちゃんプロキシーの設定を表します。
/// </summary>
public sealed class BoyomiProxySettings
{
	/// <summary>待受設定</summary>
	public ServerSettings Server { get; set; } = new ServerSettings( );

	/// <summary>棒読みちゃん接続設定</summary>
	public BoyomiSettings Boyomi { get; set; } = new BoyomiSettings( );

	/// <summary>左カメラAPI接続設定</summary>
	public HidariCameraApiSettings HidariCameraApi { get; set; } = new HidariCameraApiSettings( );

	/// <summary>レート制限設定</summary>
	public RateLimitSettings RateLimit { get; set; } = new RateLimitSettings( );

	/// <summary>ログ設定</summary>
	public ApplicationLoggingSettings Logging { get; set; } = new ApplicationLoggingSettings( );
}

/// <summary>
/// 待受設定を表します。
/// </summary>
public sealed class ServerSettings
{
	/// <summary>待受ホスト名</summary>
	public string ListenHost { get; set; } = "localhost";

	/// <summary>待受ポート番号</summary>
	public int ListenPort { get; set; } = 15080;
}

/// <summary>
/// 棒読みちゃん接続設定を表します。
/// </summary>
public sealed class BoyomiSettings
{
	/// <summary>接続先ホスト名</summary>
	public string Host { get; set; } = "localhost";

	/// <summary>接続先ポート番号</summary>
	public int Port { get; set; } = 40080;

	/// <summary>要求タイムアウト秒数</summary>
	public int TimeoutSeconds { get; set; } = 60;
}

/// <summary>
/// レート制限設定を表します。
/// </summary>
public sealed class RateLimitSettings
{
	/// <summary>ウィンドウ内の許可件数</summary>
	public int PermitLimit { get; set; } = 20;

	/// <summary>ウィンドウ秒数</summary>
	public int WindowSeconds { get; set; } = 1;

	/// <summary>待機キュー上限</summary>
	public int QueueLimit { get; set; } = 360;
}

/// <summary>
/// アプリケーションログ設定を表します。
/// </summary>
public sealed class ApplicationLoggingSettings
{
	/// <summary>ログ保持日数</summary>
	public int RetainedDays { get; set; } = 31;
}
