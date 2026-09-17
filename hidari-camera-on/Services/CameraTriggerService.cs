// Copilot作成
using System.Diagnostics;
using System.Text.Json;
using Microsoft.Extensions.Options;
using SyasaiHidariCamera.Models.Domain;
using SyasaiHidariCamera.Models.Settings;

namespace SyasaiHidariCamera.Services;

/// <summary>
/// カメラ表示の開始、延長、重複抑止、終了を管理します。
/// </summary>
public sealed class CameraTriggerService
{
	/// <summary>OBS Studioと通信するクライアント</summary>
	private readonly ObsWebSocketClient obsWebSocketClient;

	/// <summary>カメラ表示設定</summary>
	private readonly CameraSettings cameraSettings;

	/// <summary>OBS WebSocket設定</summary>
	private readonly ObsWebSocketSettings obsSettings;

	/// <summary>ログ出力を行うロガー</summary>
	private readonly ILogger<CameraTriggerService> logger;

	/// <summary>表示状態を排他制御するセマフォ</summary>
	private readonly SemaphoreSlim stateSemaphore = new(1, 1);

	/// <summary>表示タイマーを停止するトークンソース</summary>
	private CancellationTokenSource? displayCancellationTokenSource;

	/// <summary>管理対象のシーンアイテム識別子</summary>
	private int? sceneItemId;

	/// <summary>現在の表示を開始した入力元</summary>
	private CameraTriggerSource? activeSource;

	/// <summary>表示終了予定を表す単調時計の時刻</summary>
	private long displayDeadlineTimestamp;

	/// <summary>現在の表示タイマー世代</summary>
	private long timerGeneration;

	/// <summary>アプリ操作によって期待する表示状態</summary>
	private bool? expectedEnabledState;

	/// <summary>期待状態を破棄する単調時計の時刻</summary>
	private long expectedStateExpiryTimestamp;

	/// <summary>
	/// サービスを初期化します。
	/// </summary>
	/// <param name="obsWebSocketClient">OBS Studioと通信するクライアント</param>
	/// <param name="cameraOptions">カメラ表示設定を提供するオプション</param>
	/// <param name="obsOptions">OBS WebSocket設定を提供するオプション</param>
	/// <param name="logger">ログ出力を行うロガー</param>
	public CameraTriggerService(ObsWebSocketClient obsWebSocketClient, IOptions<CameraSettings> cameraOptions, IOptions<ObsWebSocketSettings> obsOptions, ILogger<CameraTriggerService> logger)
	{
		this.obsWebSocketClient = obsWebSocketClient;
		this.cameraSettings = cameraOptions.Value;
		this.obsSettings = obsOptions.Value;
		this.logger = logger;
		this.obsWebSocketClient.SceneItemStateChanged += this.OnSceneItemStateChanged;
	}

	/// <summary>
	/// 管理対象のシーンアイテム識別子を設定します。
	/// </summary>
	/// <param name="sceneItemId">管理対象のシーンアイテム識別子</param>
	public void SetSceneItemId(int sceneItemId)
	{
		this.sceneItemId = sceneItemId;
	}

	/// <summary>
	/// OBS Studioとの接続切断に伴って管理状態を初期化します。
	/// </summary>
	public void ResetConnectionState( )
	{
		this.sceneItemId = null;
		this.CancelDisplayTimer( );
		this.activeSource = null;
	}

	/// <summary>
	/// 入力元に応じてカメラ表示を開始または延長します。
	/// </summary>
	/// <param name="source">カメラの発動元</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>カメラ操作の結果</returns>
	public async Task<CameraOperationResult> TriggerAsync(CameraTriggerSource source, CancellationToken cancellationToken)
	{
		await this.stateSemaphore.WaitAsync(cancellationToken);
		try
		{
			if (this.obsWebSocketClient.State != ObsConnectionState.Ready || this.sceneItemId is null)
			{
				return new CameraOperationResult(false, false, "failed", "OBS Studioを操作できません。");
			}
			bool sceneItemEnabled = await this.GetSceneItemEnabledAsync(cancellationToken);
			if (sceneItemEnabled && this.activeSource is null)
			{
				return new CameraOperationResult(true, false, "notRequired", "手動表示中のため表示状態を変更しませんでした。");
			}
			if (sceneItemEnabled && this.activeSource != source)
			{
				return new CameraOperationResult(true, true, "notRequired", "異なる入力元による重複発動を抑止しました。");
			}
			if (sceneItemEnabled)
			{
				this.StartOrExtendDisplayTimer(source);
				return new CameraOperationResult(true, false, "extended", "同じ入力元による発動のため表示時間を延長しました。");
			}
			bool enabled = await this.SetSceneItemEnabledWithRetryAsync(true, this.cameraSettings.EnableRetryCount, this.cameraSettings.EnableRetryIntervalMilliseconds, cancellationToken);
			if (!enabled)
			{
				return new CameraOperationResult(false, false, "failed", "カメラ表示の有効化に失敗しました。");
			}
			this.activeSource = source;
			this.StartOrExtendDisplayTimer(source);
			return new CameraOperationResult(true, false, "enabled", "カメラ表示を有効化しました。");
		}
		catch (Exception exception) when (exception is not OperationCanceledException)
		{
			this.logger.LogError(exception, "カメラ表示処理に失敗しました。　詳細：{Exception}", exception);
			return new CameraOperationResult(false, false, "failed", "カメラ表示処理に失敗しました。");
		}
		finally
		{
			this.stateSemaphore.Release( );
		}
	}

	/// <summary>
	/// アプリケーション終了時に表示管理を停止し、カメラを非表示にします。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	public async Task StopAsync(CancellationToken cancellationToken)
	{
		this.CancelDisplayTimer( );
		if (this.sceneItemId is null || this.obsWebSocketClient.State == ObsConnectionState.Disconnected)
		{
			return;
		}
		await this.SetSceneItemEnabledWithRetryAsync(false, this.cameraSettings.DisableRetryCount, this.cameraSettings.DisableRetryIntervalMilliseconds, cancellationToken);
		this.activeSource = null;
	}

	/// <summary>
	/// カメラ表示の終了時刻を更新してタイマーを開始します。
	/// </summary>
	/// <param name="source">カメラの発動元</param>
	private void StartOrExtendDisplayTimer(CameraTriggerSource source)
	{
		this.activeSource = source;
		this.timerGeneration++;
		long generation = this.timerGeneration;
		this.displayDeadlineTimestamp = Stopwatch.GetTimestamp( ) + (long)(this.cameraSettings.DisplaySeconds * Stopwatch.Frequency);
		this.displayCancellationTokenSource?.Cancel( );
		this.displayCancellationTokenSource?.Dispose( );
		this.displayCancellationTokenSource = new CancellationTokenSource( );
		_ = this.RunDisplayTimerAsync(generation, this.displayCancellationTokenSource.Token);
	}

	/// <summary>
	/// 表示終了予定まで待機し、対象世代のカメラ表示を終了します。
	/// </summary>
	/// <param name="generation">表示タイマー世代</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>非同期処理を表すタスク</returns>
	private async Task RunDisplayTimerAsync(long generation, CancellationToken cancellationToken)
	{
		try
		{
			long remainingTimestamp = this.displayDeadlineTimestamp - Stopwatch.GetTimestamp( );
			if (remainingTimestamp > 0)
			{
				TimeSpan delay = TimeSpan.FromSeconds((double)remainingTimestamp / Stopwatch.Frequency);
				await Task.Delay(delay, cancellationToken);
			}
			await this.stateSemaphore.WaitAsync(cancellationToken);
			try
			{
				if (generation != this.timerGeneration || this.activeSource is null)
				{
					return;
				}
				bool disabled = await this.SetSceneItemEnabledWithRetryAsync(false, this.cameraSettings.DisableRetryCount, this.cameraSettings.DisableRetryIntervalMilliseconds, cancellationToken);
				if (disabled)
				{
					this.activeSource = null;
					this.logger.LogInformation("表示時間が終了したためカメラ表示を無効化しました。");
				}
			}
			finally
			{
				this.stateSemaphore.Release( );
			}
		}
		catch (OperationCanceledException)
		{
			// タイマー延長または終了処理による取り消しは正常終了として扱います。
		}
		catch (Exception exception)
		{
			this.logger.LogError(exception, "カメラ表示タイマーの処理に失敗しました。　詳細：{Exception}", exception);
		}
	}

	/// <summary>
	/// 管理対象の現在の表示状態を取得します。
	/// </summary>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>表示されている場合はtrue、それ以外の場合はfalse</returns>
	private async Task<bool> GetSceneItemEnabledAsync(CancellationToken cancellationToken)
	{
		JsonElement response = await this.obsWebSocketClient.SendRequestAsync("GetSceneItemEnabled", new
		{
			sceneName = this.obsSettings.SceneName,
			sceneItemId = this.sceneItemId!.Value
		}, cancellationToken);
		return response.GetProperty("sceneItemEnabled").GetBoolean( );
	}

	/// <summary>
	/// 管理対象の表示状態を再試行付きで変更します。
	/// </summary>
	/// <param name="enabled">変更後の表示状態</param>
	/// <param name="retryCount">最大試行回数</param>
	/// <param name="retryIntervalMilliseconds">再試行間隔</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>変更に成功した場合はtrue、それ以外の場合はfalse</returns>
	private async Task<bool> SetSceneItemEnabledWithRetryAsync(bool enabled, int retryCount, int retryIntervalMilliseconds, CancellationToken cancellationToken)
	{
		for (int attempt = 1; attempt <= retryCount; attempt++)
		{
			try
			{
				this.expectedEnabledState = enabled;
				this.expectedStateExpiryTimestamp = Stopwatch.GetTimestamp( ) + (long)(5 * Stopwatch.Frequency);
				await this.obsWebSocketClient.SendRequestAsync("SetSceneItemEnabled", new
				{
					sceneName = this.obsSettings.SceneName,
					sceneItemId = this.sceneItemId!.Value,
					sceneItemEnabled = enabled
				}, cancellationToken);
				return true;
			}
			catch (Exception exception) when (exception is not OperationCanceledException)
			{
				this.expectedEnabledState = null;
				this.logger.LogWarning(exception, "OBS Studioの表示状態変更に失敗しました。試行回数：{Attempt}　詳細：{Exception}", attempt, exception);
				if (attempt < retryCount)
				{
					await Task.Delay(retryIntervalMilliseconds, cancellationToken);
				}
			}
		}
		return false;
	}

	/// <summary>
	/// OBS Studioの表示状態変更を追跡します。
	/// </summary>
	/// <param name="sender">イベントの送信元</param>
	/// <param name="eventArgs">表示状態変更の情報</param>
	private void OnSceneItemStateChanged(object? sender, ObsSceneItemStateChangedEventArgs eventArgs)
	{
		if (eventArgs.SceneName != this.obsSettings.SceneName || eventArgs.SceneItemId != this.sceneItemId)
		{
			return;
		}
		bool expectedOperation = this.expectedEnabledState == eventArgs.SceneItemEnabled && Stopwatch.GetTimestamp( ) <= this.expectedStateExpiryTimestamp;
		if (expectedOperation)
		{
			this.expectedEnabledState = null;
			return;
		}
		this.expectedEnabledState = null;
		if (!eventArgs.SceneItemEnabled && this.activeSource is not null)
		{
			this.CancelDisplayTimer( );
			this.activeSource = null;
			this.logger.LogInformation("OBS Studioで手動非表示が行われたため表示管理を終了しました。");
			return;
		}
		if (eventArgs.SceneItemEnabled && this.activeSource is null)
		{
			this.logger.LogInformation("OBS Studioで手動表示が行われたため表示状態を維持します。");
		}
	}

	/// <summary>
	/// 現在の表示タイマーを取り消します。
	/// </summary>
	private void CancelDisplayTimer( )
	{
		this.timerGeneration++;
		this.displayCancellationTokenSource?.Cancel( );
		this.displayCancellationTokenSource?.Dispose( );
		this.displayCancellationTokenSource = null;
	}
}
