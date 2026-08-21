using Cane360.Application.Activities;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Activities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/activities")]
public sealed class ActivitiesController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ActivityCollectionDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ActivityCollectionDto>> Get(
        [FromQuery] Guid? fieldId,
        [FromQuery] Guid? cropCycleId,
        [FromQuery] Guid? activityTypeId,
        [FromQuery] string? status,
        [FromQuery] DateOnly? fromDate,
        [FromQuery] DateOnly? toDate,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default) =>
        Ok(await sender.Send(new GetActivitiesQuery(
            fieldId, cropCycleId, activityTypeId, status, fromDate, toDate, page, pageSize), cancellationToken));

    [HttpPost]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityDetailsDto>> Create(
        CreateActivityRequest request,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new CreateActivityCommand(
            request.FieldId,
            request.CropCycleId,
            request.ActivityTypeId,
            request.Kind,
            request.PlannedDate,
            request.SupervisorPersonId), cancellationToken);
        return CreatedAtAction(nameof(GetById), new { activityId = result.Activity.Id }, result);
    }

    [HttpGet("{activityId:guid}")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ActivityDetailsDto>> GetById(
        Guid activityId,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new GetActivityDetailsQuery(activityId), cancellationToken));

    [HttpPut("{activityId:guid}/actual-work")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivityDetailsDto>> ActualWork(
        Guid activityId,
        RecordActualWorkRequest request,
        CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseOffsetTimestamp(request.ActualAt, out var actualAt))
        {
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
            {
                [nameof(request.ActualAt)] = ["Timestamp must include Z or an explicit UTC offset."]
            }));
        }

        return Ok(await sender.Send(new RecordActualWorkCommand(
            activityId,
            request.ExpectedVersion,
            actualAt,
            request.ActualQuantity,
            request.LateEntryReason), cancellationToken));
    }

    [HttpPost("{activityId:guid}/source-references")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ActivityDetailsDto>> AddSourceReference(
        Guid activityId,
        AddSourceReferenceRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new AddSourceReferenceCommand(
            activityId,
            request.ExpectedVersion,
            request.SourceSheetReference,
            request.CapturedDate), cancellationToken));

    [HttpPost("{activityId:guid}/transitions/planned")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ActivityDetailsDto>> Planned(Guid activityId, TransitionActivityRequest request, CancellationToken cancellationToken) =>
        Transition(activityId, "Planned", request, cancellationToken);

    [HttpPost("{activityId:guid}/transitions/cancelled")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ActivityDetailsDto>> Cancelled(Guid activityId, TransitionActivityRequest request, CancellationToken cancellationToken) =>
        Transition(activityId, "Cancelled", request, cancellationToken);

    [HttpPost("{activityId:guid}/transitions/in-progress")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ActivityDetailsDto>> InProgress(Guid activityId, TransitionActivityRequest request, CancellationToken cancellationToken) =>
        Transition(activityId, "InProgress", request, cancellationToken);

    [HttpPost("{activityId:guid}/transitions/awaiting-verification")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ActivityDetailsDto>> AwaitingVerification(Guid activityId, TransitionActivityRequest request, CancellationToken cancellationToken) =>
        Transition(activityId, "AwaitingVerification", request, cancellationToken);

    [HttpPost("{activityId:guid}/transitions/manager-confirmation")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ActivityDetailsDto>> ManagerConfirmation(Guid activityId, TransitionActivityRequest request, CancellationToken cancellationToken) =>
        Transition(activityId, "ManagerConfirmation", request, cancellationToken);

    [HttpPost("{activityId:guid}/transitions/completed")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ActivityDetailsDto>> Completed(Guid activityId, TransitionActivityRequest request, CancellationToken cancellationToken) =>
        Transition(activityId, "Completed", request, cancellationToken);

    [HttpPost("{activityId:guid}/transitions/closed")]
    [ProducesResponseType<ActivityDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public Task<ActionResult<ActivityDetailsDto>> Closed(Guid activityId, TransitionActivityRequest request, CancellationToken cancellationToken) =>
        Transition(activityId, "Closed", request, cancellationToken);

    private async Task<ActionResult<ActivityDetailsDto>> Transition(
        Guid activityId,
        string targetStatus,
        TransitionActivityRequest request,
        CancellationToken cancellationToken) =>
        Ok(await sender.Send(new TransitionActivityCommand(
            activityId, targetStatus, request.ExpectedVersion, request.Reason), cancellationToken));
}
