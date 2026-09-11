// Copilot作成
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Settings;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// 音声認識セッションの直前文字列を保持します。
/// </summary>
public sealed class SpeechSessionStore
{
	/// <summary>セッション管理を排他制御する同期オブジェクト</summary>
	private readonly object synchronizationObject = new( );

	/// <summary>音声認識セッションの一覧</summary>
	private readonly Dictionary<string, SpeechSessionEntry> entries = [];

	/// <summary>サービスの動作設定</summary>
	private readonly CameraSettings settings;

	/// <summary>
	/// ストアを初期化します。
	/// </summary>
	/// <param name="options">設定を提供するオプション</param>
	public SpeechSessionStore(IOptions<CameraSettings> options)
	{
		this.settings = options.Value;
	}

	/// <summary>
	/// 直前文字列を取得し、今回文字列へ更新します。
	/// </summary>
	/// <param name="sessionId">音声認識のセッション識別子</param>
	/// <param name="originalText">受信した元の文字列</param>
	/// <param name="normalizedText">正規化済みの文字列</param>
	/// <returns>処理結果の文字列</returns>
	public string GetPreviousAndUpdate(string? sessionId, string originalText, string normalizedText)
	{
		string key = sessionId ?? string.Empty;
		lock (this.synchronizationObject)
		{
			this.RemoveExpired( );
			string previousText = this.entries.TryGetValue(key, out SpeechSessionEntry? entry) ? entry.NormalizedText : string.Empty;
			this.entries[key] = new SpeechSessionEntry(originalText, normalizedText, DateTimeOffset.UtcNow);
			this.TrimOldest( );
			return previousText;
		}
	}

	/// <summary>
	/// 期限切れセッションを削除します。
	/// </summary>
	private void RemoveExpired( )
	{
		DateTimeOffset threshold = DateTimeOffset.UtcNow.AddMinutes(-this.settings.SpeechSessionExpiryMinutes);
		foreach (string key in this.entries.Where(pair => pair.Value.LastReceivedAt < threshold).Select(pair => pair.Key).ToArray( ))
		{
			this.entries.Remove(key);
		}
	}

	/// <summary>
	/// 上限を超えた古いセッションを削除します。
	/// </summary>
	private void TrimOldest( )
	{
		while (this.entries.Count > this.settings.MaximumSpeechSessions)
		{
			string oldestKey = this.entries.MinBy(pair => pair.Value.LastReceivedAt).Key;
			this.entries.Remove(oldestKey);
		}
	}
}
