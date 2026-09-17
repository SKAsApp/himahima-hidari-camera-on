// Copilot作成
using System.Threading.Channels;
using Serilog;
using SyasaiHidariCamera.Model.Api;
using SyasaiHidariCamera.Model.Setting;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// 左カメラAPIへ送信する要求を容量制限付きで保持します。
/// </summary>
public sealed class HidariCameraQueue
{
	/// <summary>要求チャネル</summary>
	private readonly Channel<CameraEventRequest> channel;

	/// <summary>
	/// 左カメラAPI送信キューを初期化します。
	/// </summary>
	/// <param name="settings">左カメラAPI接続設定</param>
	public HidariCameraQueue(HidariCameraApiSettings settings)
	{
		BoundedChannelOptions options = new BoundedChannelOptions(settings.QueueCapacity)
		{
			FullMode = BoundedChannelFullMode.DropWrite,
			SingleReader = true,
			SingleWriter = false
		};
		this.channel = Channel.CreateBounded<CameraEventRequest>(options);
	}

	/// <summary>
	/// 要求をキューへ投入します。
	/// </summary>
	/// <param name="request">左カメラAPI要求</param>
	/// <returns>投入できた場合はtrue、それ以外の場合はfalse</returns>
	public bool TryEnqueue(CameraEventRequest request)
	{
		bool enqueued = this.channel.Writer.TryWrite(request);
		if (!enqueued)
		{
			Log.Warning("左カメラAPI送信キューが満杯のため要求を破棄しました。要求識別子：{RequestId}", request.RequestId);
		}
		return enqueued;
	}

	/// <summary>
	/// 要求をキューから順番に取得します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>要求の非同期列挙</returns>
	public IAsyncEnumerable<CameraEventRequest> ReadAllAsync(CancellationToken cancellationToken)
	{
		return this.channel.Reader.ReadAllAsync(cancellationToken);
	}

	/// <summary>
	/// キューへの書き込みを完了します。
	/// </summary>
	public void Complete( )
	{
		this.channel.Writer.TryComplete( );
	}
}
