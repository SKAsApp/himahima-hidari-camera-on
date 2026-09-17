// Copilot作成
using System.Reflection;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// アプリケーションバージョンを提供します。
/// </summary>
public sealed class ApplicationVersionService
{
	/// <summary>
	/// 実行アセンブリのバージョンを取得します。
	/// </summary>
	/// <returns>処理結果の文字列</returns>
	public string GetVersion( )
	{
		return Assembly.GetExecutingAssembly( ).GetName( ).Version?.ToString(3) ?? "1.0.0";
	}
}
