// Copilot作成
namespace SyasaiHidariCamera.Services;

/// <summary>
/// 固定トークンファイルを読み込みます。
/// </summary>
public sealed class TokenFileService
{
	/// <summary>
	/// 固定トークンを読み込みます。
	/// </summary>
	/// <param name="relativePath">実行ディレクトリーからの相対パスまたは絶対パス</param>
	/// <returns>前後の空白を除いた固定トークン</returns>
	/// <exception cref="InvalidOperationException">ファイルが存在しないか、読み取ったトークンが空の場合</exception>
	public string ReadRequiredToken(string relativePath)
	{
		string fullPath = Path.IsPathRooted(relativePath) ? relativePath : Path.Combine(AppContext.BaseDirectory, relativePath);
		if (!File.Exists(fullPath))
		{
			throw new InvalidOperationException("トークンファイルが存在しません。パス：" + fullPath);
		}

		string token = File.ReadAllText(fullPath).Trim( );
		if (string.IsNullOrWhiteSpace(token))
		{
			throw new InvalidOperationException("トークンファイルが空です。パス：" + fullPath);
		}
		return token;
	}
}
