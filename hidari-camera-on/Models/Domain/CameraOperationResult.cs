// Copilot作成
namespace SyasaiHidariCamera.Models.Domain;

/// <summary>
/// カメラ操作結果を表します。
/// </summary>
public sealed record CameraOperationResult(bool Success, bool Duplicate, string Operation, string Message);
