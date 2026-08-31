using System.Reflection.Metadata;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

namespace SyasaiHidariCamera.Model.Setting;

/// <summary>
/// 設定シングルトンクラス
/// </summary>
public class Setting
{
	/// <summary>シングルトンインスタンス</summary>
	private static Setting? singletonInstance;

	/// <summary>設定モデルインスタンス</summary>
	public GeneralSetting SettingModel {get; private set;}

	/// <summary>設定ファイルパス</summary>
	private readonly string jsonPath;

	/// <summary>
	/// 設定インスタンスを初期化します。
	/// </summary>
	private Setting( )
	{
		string exePath = Path.GetDirectoryName(AppContext.BaseDirectory)?? ".";
		this.jsonPath = exePath + "/settings/general-setting.json";
		JsonSerializerOptions jsonOption = new JsonSerializerOptions(JsonSerializerDefaults.Web)
		{
			Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
			WriteIndented = true,
			IndentCharacter = '\t'
		};
		this.SettingModel = JsonSerializer.Deserialize<GeneralSetting>(File.ReadAllText(this.jsonPath), jsonOption)?? new GeneralSetting( );
	}

	/// <summary>
	/// 設定インスタンスを生成し、返します。
	/// </summary>
	/// <returns>設定インスタンス</returns>
	public static Setting GetInstance( )
	{
		if (singletonInstance is null)
		{
			singletonInstance = new Setting( );
		}
		return singletonInstance;
	}

}
