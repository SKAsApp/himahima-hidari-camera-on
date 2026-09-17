// Copilot作成
namespace SyasaiHidariCamera.Model.Setting;

/// <summary>
/// 左カメラAPI接続設定を表します。
/// </summary>
public sealed class HidariCameraApiSettings
{
	/// <summary>コメントAPIのURL</summary>
	public string Url { get; set; } = "http://10.37.129.2:15082/api/v1/comments";

	/// <summary>接続タイムアウト秒数</summary>
	public int ConnectTimeoutSeconds { get; set; } = 2;

	/// <summary>要求全体のタイムアウト秒数</summary>
	public int RequestTimeoutSeconds { get; set; } = 5;

	/// <summary>送信キュー容量</summary>
	public int QueueCapacity { get; set; } = 360;

	/// <summary>トークンファイルパス</summary>
	public string TokenFile { get; set; } = "token/comment-api-token.txt";
}
