using System.Diagnostics;
using Serilog;

namespace SyasaiHidariCamera.Common;

/// <summary>
/// ひまひまロガーインターフェース
/// </summary>
public interface IHLogger
{
	/// <summary>要求ID</summary>
	public string RequestId {set;}

	/// <summary>
	/// デバッグログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	public void LogDebug(string message);

	/// <summary>
	/// 情報ログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	public void LogInformation(string message);

	/// <summary>
	/// 警告ログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	public void LogWarning(string message);

	/// <summary>
	/// エラーログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	public void LogError(string message);

	/// <summary>
	/// 例外ログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	/// <param name="exception">例外インスタンス</param>
	public void LogException(string message, Exception exception);

	/// <summary>
	/// ロガーを終了します。
	/// </summary>
	public void Close( );

}
