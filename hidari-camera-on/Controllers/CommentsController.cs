// Copilot作成
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SyasaiHidariCamera.Models.Api;
using SyasaiHidariCamera.Models.Domain;
using SyasaiHidariCamera.Services;

namespace SyasaiHidariCamera.Controllers;

/// <summary>
/// コメント受付APIを提供します。
/// </summary>
[ApiController]
[EnableRateLimiting("ApiPolicy")]
public sealed class CommentsController : ControllerBase
{
	/// <summary>コメント文字列を正規化するサービス</summary>
	private readonly TextNormalizationService normalizationService;

	/// <summary>コメントの発動条件を判定するサービス</summary>
	private readonly CommentTriggerMatcher matcher;

	/// <summary>カメラの表示処理を実行するサービス</summary>
	private readonly CameraTriggerService triggerService;

	/// <summary>
	/// コントローラーを初期化します。
	/// </summary>
	/// <param name="normalizationService">コメント文字列を正規化するサービス</param>
	/// <param name="matcher">コメントの発動条件を判定するサービス</param>
	/// <param name="triggerService">カメラの表示処理を実行するサービス</param>
	public CommentsController(TextNormalizationService normalizationService, CommentTriggerMatcher matcher, CameraTriggerService triggerService)
	{
		this.normalizationService = normalizationService;
		this.matcher = matcher;
		this.triggerService = triggerService;
	}

	/// <summary>
	/// コメントを受け付けて発動判定します。
	/// </summary>
	/// <param name="request">コメント受付APIの要求情報</param>
	/// <param name="cancellationToken">非同期処理の取り消しを通知するトークン</param>
	/// <returns>コメントの受付結果とカメラ操作結果</returns>
	[HttpPost("/api/v1/comments")]
	public async Task<ActionResult<CameraEventResponse>> PostAsync([FromBody] CameraEventRequest request, CancellationToken cancellationToken)
	{
		// 要求識別子または受信日時が不正か、送信元が想定外の場合は不正要求にします。
		if (!request.HasValidIdentifiers( ) || request.Source != "boyomi-proxy")
		{
			return this.BadRequest( );
		}

		string normalizedText = this.normalizationService.NormalizeComment(request.Text);

		// 正規化後のコメントが発動条件に一致しない場合は、OBS Studioを操作せず正常応答にします。
		if (!this.matcher.IsMatch(normalizedText))
		{
			return this.Ok(CreateResponse(request.RequestId, false, false, "notRequired", "発動条件に一致しませんでした。"));
		}

		CameraOperationResult result = await this.triggerService.TriggerAsync(CameraTriggerSource.Comment, cancellationToken);

		// OBS Studioの操作に失敗した場合は、一時的に処理できないためHTTP 503を返します。
		if (!result.Success)
		{
			return this.Problem(statusCode: 503, title: "OBS Studioを操作できません。", detail: result.Message);
		}

		// OBS Studioの操作に成功した場合は、操作結果を含む正常応答を返します。
		return this.Ok(CreateResponse(request.RequestId, true, result.Duplicate, result.Operation, result.Message));
	}

	/// <summary>
	/// API応答を生成します。
	/// </summary>
	/// <param name="requestId">要求識別子</param>
	/// <param name="triggered">カメラ表示の発動有無</param>
	/// <param name="duplicate">重複要求として扱ったかどうか</param>
	/// <param name="operation">OBS Studio操作の結果</param>
	/// <param name="message">処理結果の説明</param>
	/// <returns>生成したAPI応答</returns>
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
