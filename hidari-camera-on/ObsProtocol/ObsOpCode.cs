// Copilot作成
namespace SyasaiHidariCamera.Services;

/// <summary>
/// OBS WebSocket操作コードを表します。
/// </summary>
public enum ObsOpCode
{
	/// <summary>Helloを表す値</summary>
	Hello = 0,

	/// <summary>Identifyを表す値</summary>
	Identify = 1,

	/// <summary>Identifiedを表す値</summary>
	Identified = 2,

	/// <summary>Eventを表す値</summary>
	Event = 5,

	/// <summary>Requestを表す値</summary>
	Request = 6,

	/// <summary>RequestResponseを表す値</summary>
	RequestResponse = 7
}
