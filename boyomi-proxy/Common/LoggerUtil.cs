using Serilog;

namespace SyasaiHidariCamera.Common;

/// <summary>
/// Serilogを管理します。
/// </summary>
public class LoggerUtil
{
	/// <summary>
	/// Serilogを設定します。
	/// </summary>
	public void InitialiseLogger( )
	{
		string logFilePath = Path.Combine(Path.GetDirectoryName(AppContext.BaseDirectory)?? "", "./log/");
		Log.Logger = new LoggerConfiguration( )
			.MinimumLevel.Debug( )
			.WriteTo.Console( )
			.WriteTo.File(
				logFilePath + ".log", 
				rollingInterval: RollingInterval.Day, 
				restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Debug, 
				retainedFileCountLimit: 4096, 
				retainedFileTimeLimit: new TimeSpan(31, 0, 0, 0))
			.CreateLogger( );
	}

}
