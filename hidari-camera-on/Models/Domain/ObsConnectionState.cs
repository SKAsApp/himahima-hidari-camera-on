// Copilot作成
namespace SyasaiHidariCamera.Models.Domain;

/// <summary>
/// OBS接続状態を表します。
/// </summary>
public enum ObsConnectionState
{
	/// <summary>Disconnectedを表す値</summary>
	Disconnected,

	/// <summary>Connectingを表す値</summary>
	Connecting,

	/// <summary>Identifyingを表す値</summary>
	Identifying,

	/// <summary>ConnectedNotReadyを表す値</summary>
	ConnectedNotReady,

	/// <summary>Readyを表す値</summary>
	Ready,

	/// <summary>Stoppingを表す値</summary>
	Stopping
}
