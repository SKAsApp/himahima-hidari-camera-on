// Copilot作成
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyasaiHidariCamera.Models.Api;
using SyasaiHidariCamera.Models.Domain;
using SyasaiHidariCamera.Services;

namespace SyasaiHidariCamera.Controllers;

/// <summary>
/// 音声認識受付APIを提供します。
/// </summary>
[ApiController]
[EnableRateLimiting("ApiPolicy")]
public sealed class SpeechRecognitionController : ControllerBase
{

	/// <summary>normalizationServiceの値</summary>
	private readonly TextNormalizationService normalizationService;

	/// <summary>matcherの値</summary>
	private readonly SpeechTriggerMatcher matcher;

	/// <summary>sessionStoreの値</summary>
	private readonly SpeechSessionStore sessionStore;

	/// <summary>triggerServiceの値</summary>
	private readonly CameraTriggerService triggerService;

	/// <summary>
	/// コントローラーを初期化します。
	/// </summary>
	/// <param name="normalizationService">文字列を正規化するサービス</param>
	/// <param name="matcher">発動条件を判定するサービス</param>
	/// <param name="sessionStore">sessionStoreの値</param>
	/// <param name="triggerService">カメラ表示処理を実行するサービス</param>
	public SpeechRecognitionController(TextNormalizationService normalizationService, SpeechTriggerMatcher matcher, SpeechSessionStore sessionStore, CameraTriggerService triggerService)
	{
		this.normalizationService = normalizationService;
		this.matcher = matcher;
		this.sessionStore = sessionStore;
		this.triggerService = triggerService;
	}

	/// <summary>
	/// 音声認識文字列を受け付けて発動判定します。
	/// </summary>
	/// <param name="request">APIの要求情報</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>HTTP応答</returns>
	[HttpPost("/api/v1/speech-recognition")]
	public async Task<ActionResult<CameraEventResponse>> PostAsync([FromBody] CameraEventRequest request, CancellationToken cancellationToken)
	{
		if (!request.HasValidIdentifiers( ) || request.Source != "speech-recognition-telop")
		{
			return this.BadRequest( );
		}
		string normalizedText = this.normalizationService.NormalizeSpeech(request.Text);
		string previousText = this.sessionStore.GetPreviousAndUpdate(request.SessionId, request.Text, normalizedText);
		if (!this.matcher.IsMatch(previousText, normalizedText))
		{
			return this.Ok(CreateResponse(request.RequestId, false, false, "notRequired", "発動条件に一致しませんでした。"));
		}
		CameraOperationResult result = await this.triggerService.TriggerAsync(CameraTriggerSource.SpeechRecognition, cancellationToken);
		if (!result.Success)
		{
			return this.Problem(statusCode: 503, title: "OBS Studioを操作できません。", detail: result.Message);
		}
		return this.Ok(CreateResponse(request.RequestId, true, result.Duplicate, result.Operation, result.Message));
	}

	/// <summary>
	/// API応答を生成します。
	/// </summary>
	/// <param name="requestId">requestIdの値</param>
	/// <param name="triggered">triggeredの値</param>
	/// <param name="duplicate">duplicateの値</param>
	/// <param name="operation">operationの値</param>
	/// <param name="message">messageの値</param>
	/// <returns>処理結果</returns>
	private static CameraEventResponse CreateResponse(string requestId, bool triggered, bool duplicate, string operation, string message)
	{
		return new CameraEventResponse
		{
			RequestId = requestId,
			Accepted = true,
			Triggered = triggered,
			Duplicate = duplicate,
			ObsOperation = operation,
			Message = message
		};
	}
}
