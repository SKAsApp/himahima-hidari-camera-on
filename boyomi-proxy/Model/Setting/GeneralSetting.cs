using System.Text.Json.Serialization;

namespace SyasaiHidariCamera.Model.Setting;

/// <summary>
/// 一般設定モデル
/// </summary>
public class GeneralSetting
{
	/// <summary>プロキシー待ち受けポート。初期値：10080</summary>
	public int ListenPort {get; set;} = 10080;

	/// <summary>棒読みちゃんのホスト名。初期値：localhost</summary>
	public string BoyomiHost {get; set;} = "localhost";

	/// <summary>棒読みちゃんのポート。初期値：50080</summary>
	public int BoyomiPort {get; set;} = 50080;

	/// <summary>
	/// 初期値を指定して一般設定モデルインスタンスを生成します。
	/// </summary>
	/// <param name="listenPort">プロキシー待ち受けポート</param>
	/// <param name="boyomiHost">棒読みちゃんのホスト名</param>
	/// <param name="boyomiPort">棒読みちゃんのポート</param>
	[JsonConstructor]
	public GeneralSetting(int listenPort, string boyomiHost, int boyomiPort)
	{
		this.ListenPort = listenPort;
		this.BoyomiHost = boyomiHost;
		this.BoyomiPort = boyomiPort;
	}

	public GeneralSetting( )
	{
		
	}

}
