// Copilot作成
namespace SyasaiHidariCamera.Models.Domain;

/// <summary>
/// OBS Studioのシーンアイテム表示状態変更イベントを表します。
/// </summary>
public sealed class ObsSceneItemStateChangedEventArgs : EventArgs
{
	/// <summary>シーン名</summary>
	public required string SceneName { get; init; }

	/// <summary>シーンアイテム識別子</summary>
	public int SceneItemId { get; init; }

	/// <summary>変更後の表示状態</summary>
	public bool SceneItemEnabled { get; init; }
}
