// Copilot作成
using SyasaiHidariCamera.Models.Domain;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// 発動判定結果をOBS Studio操作へ橋渡しします。
/// </summary>
public sealed class CameraTriggerService
{
	/// <summary>OBS Studioと通信するクライアント</summary>
	private readonly ObsWebSocketClient obsWebSocketClient;

	/// <summary>
	/// サービスを初期化します。
	/// </summary>
	/// <param name="obsWebSocketClient">obsWebSocketClientの値</param>
	public CameraTriggerService(ObsWebSocketClient obsWebSocketClient)
	{
		this.obsWebSocketClient = obsWebSocketClient;
	}

	/// <summary>
	/// カメラ有効化を最大3回再試行します。
	/// </summary>
	/// <param name="source">カメラの発動元</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>カメラ操作の結果</returns>
	public async Task<CameraOperationResult> TriggerAsync(CameraTriggerSource source, CancellationToken cancellationToken)
	{
		if (this.obsWebSocketClient.State != ObsConnectionState.Ready)
		{
			return new CameraOperationResult(false, false, "failed", "OBS Studioを操作できません。");
		}
		// 70パーセント段階では表示時間管理を未実装とし、有効化要求の入口を確定します。
		return new CameraOperationResult(false, false, "failed", "OBS Studio表示管理は次段階で実装します。");
	}
}
