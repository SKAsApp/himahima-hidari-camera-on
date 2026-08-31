using System.Diagnostics;
using InputSimulatorEx;
using InputSimulatorEx.Native;
using SyasaiHidariCamera.Common;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// ホットキー送信サービス
/// </summary>
public class HotKeySendService
{
	/// <summary>ロガー</summary>
	private readonly IHLogger logger;

	/// <summary>入力シミュレーター</summary>
	private readonly InputSimulator inputSimulator = new InputSimulator();

	/// <summary>
	/// ホットキー送信サービスを初期化します。
	/// </summary>
	public HotKeySendService(IHLogger logger)
	{
		this.logger = logger;
	}

	/// <summary>
	/// ホットキーを別プロセスへ送信します。
	/// </summary>
	/// <param name="processName">プロセス名</param>
	/// <param name="number">ホットキーの番号</param>
	public void SendHotKey(string processName1, string processName2, int number)
	{
		// processName1にフォーカスを当て、ホットキーを送信
		this.ChangeForcusWindow(processName1);
		this.SendCtrlNumPad(number);
		// processName2にフォーカスを戻す
		if (processName2 == "")
		{
			return;
		}
		this.ChangeForcusWindow(processName2);
	}

	public void ChangeForcusWindow(string processName)
	{
		this.logger.LogDebug("プロセス「" + processName + "」のウィンドウにフォーカスを変更します。");
		try
		{
			// プロセスの取得
			Process? process = Process.GetProcessesByName(processName).FirstOrDefault();
			if (process is null)
			{
				this.logger.LogWarning("プロセス「" + processName + "」が見つかりませんでした。");
				return;
			}
			// ウィンドウを探す
			nint windowSendTo = process.MainWindowHandle;
			if (windowSendTo == nint.Zero)
			{
				this.logger.LogWarning("プロセス「" + processName + "」のウィンドウが見つかりませんでした。");
				return;
			}
			// フォーカスを切り替え
			WindowsNativeMethods.SetForegroundWindow(windowSendTo);
		}
		catch (Exception e)
		{
			this.logger.LogException("プロセス「" + processName + "」のウィンドウへのフォーカス変更中にエラーが発生しました。", e);
		}
	}

	/// <summary>
	/// Ctrl＋数字キーを送ります。
	/// </summary>
	/// <param name="numPad">送信する数字キー</param>
	public void SendCtrlNumPad(int number)
	{
		this.logger.LogDebug("「Control＋NumPad" + number.ToString( ) + "」キーを送出します。");
		try
		{
			if (number == 1)
			{
				this.logger.LogDebug("Control＋NumPad1");
				this.inputSimulator.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL }, VirtualKeyCode.NUMPAD1);
				return;
			}
			if (number == 2)
			{
				this.logger.LogDebug("Control＋NumPad2");
				this.inputSimulator.Keyboard.ModifiedKeyStroke(new[] { VirtualKeyCode.CONTROL }, VirtualKeyCode.NUMPAD2);
				return;
			}
		}
		catch (Exception e)
		{
			this.logger.LogException("「Control＋NumPad" + number.ToString( ) + "」キーの送出中にエラーが発生しました。", e);
		}
	}

}
