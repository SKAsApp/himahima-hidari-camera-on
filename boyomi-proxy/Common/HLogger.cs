using System.Diagnostics;
using Serilog;

namespace SyasaiHidariCamera.Common;

/// <summary>
/// ひまひまロガー
/// </summary>
public class HLogger: IHLogger
{
	/// <summary>実行ID</summary>
	private string executionId;

	/// <summary>要求ID</summary>
	public string RequestId {private get; set;} = "";

	/// <summary>デバッグモード</summary>
	private bool debugMode;

	/// <summary>
	/// ひまひまロガーを生成します。
	/// </summary>
	/// <param name="executionId">実行ID</param>
	/// <param name="debugMode">デバッグモードにする</param>
	public HLogger(string executionId, bool debugMode)
	{
		this.executionId = executionId;
		this.debugMode = debugMode;
		new LoggerUtil( ).InitialiseLogger( );
	}

	/// <summary>
	/// デバッグログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	public void LogDebug(string message)
	{
		if (!debugMode)
		{
			return;
		}
		string callerClass = this.GetCallerClassNames( );
		string logMessage = this.RequestId == ""?
			"[" + this.executionId + "]【" + callerClass + "】 " + message:
			"[" + this.executionId + "]〔" + this.RequestId + "〕【" + callerClass + "】 " + message;
		Log.Debug(logMessage);
	}

	/// <summary>
	/// 情報ログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	public void LogInformation(string message)
	{
		string callerClass = this.GetCallerClassNames( );
		string logMessage = this.RequestId == ""?
			"[" + this.executionId + "]【" + callerClass + "】 " + message:
			"[" + this.executionId + "]〔" + this.RequestId + "〕【" + callerClass + "】 " + message;
		Log.Information(logMessage);
	}

	/// <summary>
	/// 警告ログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	public void LogWarning(string message)
	{
		string callerClass = this.GetCallerClassNames( );
		string logMessage = this.RequestId == ""?
			"[" + this.executionId + "]【" + callerClass + "】 " + message:
			"[" + this.executionId + "]〔" + this.RequestId + "〕【" + callerClass + "】 " + message;
		Log.Warning(logMessage);
	}

	/// <summary>
	/// エラーログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	public void LogError(string message)
	{
		string callerClass = this.GetCallerClassNames( );
		string logMessage = this.RequestId == ""?
			"[" + this.executionId + "]【" + callerClass + "】 " + message:
			"[" + this.executionId + "]〔" + this.RequestId + "〕【" + callerClass + "】 " + message;
		Log.Error(logMessage);
	}

	/// <summary>
	/// 例外ログを出力します。
	/// </summary>
	/// <param name="message">ログメッセージ</param>
	/// <param name="exception">例外インスタンス</param>
	public void LogException(string message, Exception exception)
	{
		string callerClass = this.GetCallerClassNames( );
		string logMessage = this.RequestId == ""?
			"[" + this.executionId + "]【" + callerClass + "】 " + message + "　詳細：" + exception.ToString( ):
			"[" + this.executionId + "]〔" + this.RequestId + "〕【" + callerClass + "】 " + message + "　詳細：" + exception.ToString( );
		Log.Error(logMessage);
	}

	/// <summary>
	/// ログ呼び出し元クラスを取得します。
	/// </summary>
	/// <returns>ログ呼び出し元クラス名</returns>
	private string GetCallerClassNames( )
	{
		string[ ] tempCallerClassNames = (new StackFrame(2, false).GetMethod( )?.DeclaringType?.FullName?? "").Split('+')[0].Split('.');
		return tempCallerClassNames[tempCallerClassNames.Length - 1] == "StackFrame"?
			"Program":
			tempCallerClassNames[tempCallerClassNames.Length - 1];	
	}

	/// <summary>
	/// ロガーを終了します。
	/// </summary>
	public void Close( )
	{
		Log.CloseAndFlush( );
	}

}
