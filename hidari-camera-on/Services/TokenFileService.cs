// Copilot作成
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Settings;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// 秘密情報ファイルを読み込みます。
/// </summary>
public sealed class TokenFileService
{
	/// <summary>セキュリティ設定</summary>
	private readonly SecuritySettings securitySettings;

	/// <summary>OBS WebSocket設定</summary>
	private readonly ObsWebSocketSettings obsSettings;

	/// <summary>
	/// サービスを初期化します。
	/// </summary>
	/// <param name="securityOptions">セキュリティ設定を提供するオプション</param>
	/// <param name="obsOptions">OBS WebSocket設定を提供するオプション</param>
	public TokenFileService(IOptions<SecuritySettings> securityOptions, IOptions<ObsWebSocketSettings> obsOptions)
	{
		this.securitySettings = securityOptions.Value;
		this.obsSettings = obsOptions.Value;
	}

	/// <summary>
	/// 必須の秘密情報をすべて読み込みます。
	/// </summary>
	/// <returns>処理結果</returns>
	public TokenStore LoadRequiredTokens( )
	{
		return new TokenStore(this.ReadRequired(this.securitySettings.CommentTokenFile), this.ReadRequired(this.securitySettings.SpeechTokenFile), this.ReadRequired(this.obsSettings.PasswordFile));
	}

	/// <summary>
	/// 必須ファイルを読み込みます。
	/// </summary>
	/// <param name="filePath">読み込むファイルのパス</param>
	/// <returns>処理結果の文字列</returns>
	private string ReadRequired(string filePath)
	{
		if (!File.Exists(filePath))
		{
			throw new FileNotFoundException($"必須ファイルがありません。対象：{filePath}", filePath);
		}
		string value = File.ReadAllText(filePath).Trim( );
		if (string.IsNullOrWhiteSpace(value))
		{
			throw new InvalidDataException($"必須ファイルが空です。対象：{filePath}");
		}
		return value;
	}
}
