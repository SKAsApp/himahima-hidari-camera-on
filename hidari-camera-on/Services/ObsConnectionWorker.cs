// Copilot作成
using System.Net.WebSockets;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Settings;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// OBS Studioへの接続、再接続、管理対象の探索を継続します。
/// </summary>
public sealed class ObsConnectionWorker : BackgroundService
{
	/// <summary>OBS Studioと通信するクライアント</summary>
	private readonly ObsWebSocketClient obsWebSocketClient;

	/// <summary>カメラ表示を管理するサービス</summary>
	private readonly CameraTriggerService cameraTriggerService;

	/// <summary>OBS WebSocket設定</summary>
	private readonly ObsWebSocketSettings settings;

	/// <summary>ログ出力を行うロガー</summary>
	private readonly ILogger<ObsConnectionWorker> logger;

	/// <summary>
	/// ワーカーを初期化します。
	/// </summary>
	/// <param name="obsWebSocketClient">OBS Studioと通信するクライアント</param>
	/// <param name="cameraTriggerService">カメラ表示を管理するサービス</param>
	/// <param name="options">OBS WebSocket設定を提供するオプション</param>
	/// <param name="logger">ログ出力を行うロガー</param>
	public ObsConnectionWorker(ObsWebSocketClient obsWebSocketClient, CameraTriggerService cameraTriggerService, IOptions<ObsWebSocketSettings> options, ILogger<ObsConnectionWorker> logger)
	{
		this.obsWebSocketClient = obsWebSocketClient;
		this.cameraTriggerService = cameraTriggerService;
		this.settings = options.Value;
		this.logger = logger;
	}

	/// <summary>
	/// OBS Studioへの接続と再接続を実行します。
	/// </summary>
	/// <param name="stoppingToken">ワーカーの停止を通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				await this.obsWebSocketClient.ConnectAsync(stoppingToken);
				this.logger.LogInformation("OBS Studioへ接続しました。");
				Task receiveTask = this.obsWebSocketClient.RunReceiveLoopAsync(stoppingToken);
				await this.PrepareTargetAsync(receiveTask, stoppingToken);
				await receiveTask;
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				// Control-Cによる停止要求は正常終了として扱います。
				return;
			}
			catch (Exception) when (stoppingToken.IsCancellationRequested)
			{
				// 停止処理とWebSocket受信が競合した場合の例外も正常終了として扱います。
				return;
			}
			catch (Exception exception)
			{
				this.logger.LogError(exception, "OBS Studioとの接続処理に失敗しました。　詳細：{Exception}", exception);
			}
			finally
			{
				this.cameraTriggerService.ResetConnectionState( );
			}
			try
			{
				await Task.Delay(TimeSpan.FromSeconds(this.settings.ReconnectIntervalSeconds), stoppingToken);
			}
			catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
			{
				// 再接続待機中の停止要求は正常終了として扱います。
				return;
			}
		}
	}

	/// <summary>
	/// アプリケーション終了時にカメラを非表示にして接続を終了します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	public override async Task StopAsync(CancellationToken cancellationToken)
	{
		await this.cameraTriggerService.StopAsync(cancellationToken);
		await this.obsWebSocketClient.DisconnectAsync(cancellationToken);
		await base.StopAsync(cancellationToken);
	}

	/// <summary>
	/// 管理対象を探索し、起動時の表示状態を無効化します。
	/// </summary>
	/// <param name="receiveTask">WebSocket受信処理を表すタスク</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	private async Task PrepareTargetAsync(Task receiveTask, CancellationToken cancellationToken)
	{
		while (!cancellationToken.IsCancellationRequested && !receiveTask.IsCompleted)
		{
			try
			{
				JsonElement response = await this.obsWebSocketClient.SendRequestAsync("GetSceneItemId", new
				{
					sceneName = this.settings.SceneName,
					sourceName = this.settings.SourceName
				}, cancellationToken);
				int sceneItemId = response.GetProperty("sceneItemId").GetInt32( );
				this.cameraTriggerService.SetSceneItemId(sceneItemId);
				await this.obsWebSocketClient.SendRequestAsync("SetSceneItemEnabled", new
				{
					sceneName = this.settings.SceneName,
					sceneItemId,
					sceneItemEnabled = false
				}, cancellationToken);
				this.obsWebSocketClient.MarkReady( );
				this.logger.LogInformation("管理対象のOBS Studioシーンアイテムを準備しました。シーン：{SceneName}、ソース：{SourceName}、識別子：{SceneItemId}", this.settings.SceneName, this.settings.SourceName, sceneItemId);
				return;
			}
			catch (Exception exception) when (exception is not OperationCanceledException && exception is not WebSocketException)
			{
				this.logger.LogWarning(exception, "管理対象のOBS Studioシーンアイテムを取得できませんでした。15秒後に再試行します。　詳細：{Exception}", exception);
			}
			await Task.Delay(TimeSpan.FromSeconds(this.settings.ReconnectIntervalSeconds), cancellationToken);
		}
		await receiveTask;
	}
}
