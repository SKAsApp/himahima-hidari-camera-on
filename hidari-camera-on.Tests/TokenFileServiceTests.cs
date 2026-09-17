// Copilot作成
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Settings;
using SyasaiHidariCamera.Services;
using Xunit;

namespace SyasaiHidariCamera.Tests;

/// <summary>
/// TokenFileServiceの必須秘密情報読み込みを検証します。
/// </summary>
public sealed class TokenFileServiceTests : IDisposable
{
	/// <summary>テスト用一時ディレクトリー</summary>
	private readonly string temporaryDirectory = Path.Combine(Path.GetTempPath( ), Guid.NewGuid( ).ToString( ));

	/// <summary>
	/// テスト用一時ディレクトリーを作成します。
	/// </summary>
	public TokenFileServiceTests( )
	{
		Directory.CreateDirectory(this.temporaryDirectory);
	}

	/// <summary>
	/// すべてのファイルが存在する場合に前後の空白を除去して読み込むことを検証します。
	/// </summary>
	[Fact]
	public void LoadRequiredTokens_WhenAllFilesExist_ReturnsTrimmedValues( )
	{
		TokenFileService service = this.CreateService(" comment-token \n", " speech-token \n", " obs-password \n");
		TokenStore actual = service.LoadRequiredTokens( );
		Assert.Equal("comment-token", actual.CommentToken);
		Assert.Equal("speech-token", actual.SpeechToken);
		Assert.Equal("obs-password", actual.ObsPassword);
	}

	/// <summary>
	/// 必須ファイルが存在しない場合に例外を通知することを検証します。
	/// </summary>
	[Fact]
	public void LoadRequiredTokens_WhenRequiredFileDoesNotExist_ThrowsFileNotFoundException( )
	{
		SecuritySettings securitySettings = new( )
		{
			CommentTokenFile = Path.Combine(this.temporaryDirectory, "missing.txt"),
			SpeechTokenFile = this.WriteFile("speech.txt", "speech-token")
		};
		ObsWebSocketSettings obsSettings = new( )
		{
			PasswordFile = this.WriteFile("obs.txt", "obs-password")
		};
		TokenFileService service = new(Options.Create(securitySettings), Options.Create(obsSettings));
		Assert.Throws<FileNotFoundException>(() => service.LoadRequiredTokens( ));
	}

	/// <summary>
	/// 必須ファイルが空の場合に例外を通知することを検証します。
	/// </summary>
	[Fact]
	public void LoadRequiredTokens_WhenRequiredFileIsEmpty_ThrowsInvalidDataException( )
	{
		TokenFileService service = this.CreateService("", "speech-token", "obs-password");
		Assert.Throws<InvalidDataException>(() => service.LoadRequiredTokens( ));
	}

	/// <summary>
	/// テストで使用した一時ファイルを削除します。
	/// </summary>
	public void Dispose( )
	{
		Directory.Delete(this.temporaryDirectory, true);
	}

	/// <summary>
	/// 指定した秘密情報を持つテスト対象を生成します。
	/// </summary>
	/// <param name="commentToken">コメントAPI用トークン</param>
	/// <param name="speechToken">音声認識API用トークン</param>
	/// <param name="obsPassword">OBS WebSocketパスワード</param>
	/// <returns>生成したトークンファイルサービス</returns>
	private TokenFileService CreateService(string commentToken, string speechToken, string obsPassword)
	{
		SecuritySettings securitySettings = new( )
		{
			CommentTokenFile = this.WriteFile("comment.txt", commentToken),
			SpeechTokenFile = this.WriteFile("speech.txt", speechToken)
		};
		ObsWebSocketSettings obsSettings = new( )
		{
			PasswordFile = this.WriteFile("obs.txt", obsPassword)
		};
		return new TokenFileService(Options.Create(securitySettings), Options.Create(obsSettings));
	}

	/// <summary>
	/// 一時ファイルへ値を書き込みます。
	/// </summary>
	/// <param name="fileName">ファイル名</param>
	/// <param name="content">書き込む内容</param>
	/// <returns>作成したファイルのパス</returns>
	private string WriteFile(string fileName, string content)
	{
		string filePath = Path.Combine(this.temporaryDirectory, fileName);
		File.WriteAllText(filePath, content);
		return filePath;
	}
}
