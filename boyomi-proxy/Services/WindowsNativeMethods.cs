using System.Runtime.InteropServices;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// Windowsのネイティブメソッドを提供します。
/// </summary>
internal class WindowsNativeMethods
{
	[StructLayout(LayoutKind.Sequential)]
	public struct INPUT
	{
		public uint type;
		public InputUnion u;
	}

	[StructLayout(LayoutKind.Explicit)]
	public struct InputUnion
	{
		[FieldOffset(0)] public KEYBDINPUT ki;
	}

	[StructLayout(LayoutKind.Sequential)]
	public struct KEYBDINPUT
	{
		public ushort wVk;
		public ushort wScan;
		public uint dwFlags;
		public uint time;
		public IntPtr dwExtraInfo;
	}

	public const uint INPUT_KEYBOARD = 1;
	public const uint KEYEVENTF_KEYUP = 0x0002;
	public const uint KEYEVENTF_SCANCODE = 0x0008;

	[DllImport("user32.dll")]
	public static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

	[DllImport("user32.dll")]
	public static extern bool SetForegroundWindow(IntPtr hWnd);

	public const int WM_KEYDOWN = 0x0100;
	public const int WM_CHAR = 0x0102;
	public const int WM_SYSKEYDOWN = 0x0104;

	public delegate IntPtr SubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam, IntPtr uIdSubclass, IntPtr dwRefData);

	[DllImport("ComCtl32.dll")]
	public static extern bool SetWindowSubclass(IntPtr hWnd, SubclassProc pfnSubclass, uint uIdSubclass, IntPtr dwRefData);

	[DllImport("ComCtl32.dll")]
	public static extern IntPtr DefSubclassProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

}
