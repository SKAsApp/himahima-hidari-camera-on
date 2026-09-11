// Copilot作成
namespace SyasaiHidariCamera.Services;

/// <summary>
/// 音声認識セッションで保持する文字列と受信日時を表します。
/// </summary>
internal sealed record SpeechSessionEntry(string OriginalText, string NormalizedText, DateTimeOffset LastReceivedAt);
