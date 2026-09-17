// Copilot作成
namespace SyasaiHidariCamera.Services;

/// <summary>
/// 読み込んだ秘密情報を保持します。
/// </summary>
public sealed record TokenStore(string CommentToken, string SpeechToken, string ObsPassword);
