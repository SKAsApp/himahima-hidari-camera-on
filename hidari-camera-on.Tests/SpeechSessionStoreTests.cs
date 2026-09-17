// Copilot作成
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Settings;
using SyasaiHidariCamera.Services;
using Xunit;

namespace SyasaiHidariCamera.Tests;

/// <summary>
/// SpeechSessionStoreのセッション単位の直前文字列保持を検証します。
/// </summary>
public sealed class SpeechSessionStoreTests
{
	/// <summary>
	/// 同じセッションの前回文字列を返して今回文字列へ更新することを検証します。
	/// </summary>
	[Fact]
	public void GetPreviousAndUpdate_WhenSessionExists_ReturnsPreviousText( )
	{
		SpeechSessionStore store = this.CreateStore(10);
		string first = store.GetPreviousAndUpdate("session-1", "左カメラ", "左カメラ");
		string second = store.GetPreviousAndUpdate("session-1", "ON", "ON");
		Assert.Equal(string.Empty, first);
		Assert.Equal("左カメラ", second);
	}

	/// <summary>
	/// 異なるセッションの文字列を混在させないことを検証します。
	/// </summary>
	[Fact]
	public void GetPreviousAndUpdate_WhenSessionDiffers_ReturnsEmptyText( )
	{
		SpeechSessionStore store = this.CreateStore(10);
		store.GetPreviousAndUpdate("session-1", "左カメラ", "左カメラ");
		string actual = store.GetPreviousAndUpdate("session-2", "ON", "ON");
		Assert.Equal(string.Empty, actual);
	}

	/// <summary>
	/// 最大セッション数を超えた場合に最も古いセッションを削除することを検証します。
	/// </summary>
	[Fact]
	public void GetPreviousAndUpdate_WhenMaximumIsExceeded_RemovesOldestSession( )
	{
		SpeechSessionStore store = this.CreateStore(1);
		store.GetPreviousAndUpdate("session-1", "左", "左");
		store.GetPreviousAndUpdate("session-2", "右", "右");
		string actual = store.GetPreviousAndUpdate("session-1", "カメラ", "カメラ");
		Assert.Equal(string.Empty, actual);
	}

	/// <summary>
	/// 指定した最大セッション数でテスト対象を生成します。
	/// </summary>
	/// <param name="maximumSpeechSessions">音声認識セッションの最大数</param>
	/// <returns>生成したセッションストア</returns>
	private SpeechSessionStore CreateStore(int maximumSpeechSessions)
	{
		CameraSettings settings = new( )
		{
			MaximumSpeechSessions = maximumSpeechSessions,
			SpeechSessionExpiryMinutes = 30
		};
		return new SpeechSessionStore(Options.Create(settings));
	}
}
