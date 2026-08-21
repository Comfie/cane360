using Cane360.Application.Labour;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Labour;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/work-records")]
public sealed class WorkRecordsController(ISender sender) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<WorkRecordDto>>> Get(
        [FromQuery] string? workDate, [FromQuery] Guid? workerId, [FromQuery] Guid? activityId,
        CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseOptionalDateOnly(workDate, out var parsedDate))
        {
            return BadRequest(DateError(nameof(workDate)));
        }

        return Ok(await sender.Send(new GetWorkRecordsQuery(parsedDate, workerId, activityId), cancellationToken));
    }

    [HttpGet("reference-data")]
    public async Task<ActionResult<LabourReferenceDataDto>> ReferenceData([FromQuery] string workDate, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(workDate, out var parsedDate))
        {
            return BadRequest(DateError(nameof(workDate)));
        }

        return Ok(await sender.Send(new GetLabourReferenceDataQuery(parsedDate), cancellationToken));
    }

    [HttpPost]
    public async Task<ActionResult<WorkRecordDto>> Create(CreateWorkRecordRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.WorkDate, out var workDate))
        {
            return BadRequest(DateError(nameof(request.WorkDate)));
        }

        var result = await sender.Send(new CreateWorkRecordCommand(request.WorkerId, workDate,
            request.PayBasis, request.ActivityIds, request.Quantity, Scope(request.Scope), request.LateEntryReason), cancellationToken);
        return CreatedAtAction(nameof(Get), new { workDate = result.WorkDate, workerId = result.WorkerId }, result);
    }

    [HttpPost("{workRecordId:guid}/supervisor-verification")]
    public async Task<ActionResult<WorkRecordDto>> Verify(Guid workRecordId, VerifyWorkRecordRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new VerifyWorkRecordCommand(workRecordId, request.SupervisorPersonId, request.ExpectedVersion), cancellationToken));

    [HttpPost("{workRecordId:guid}/manager-confirmation")]
    public async Task<ActionResult<WorkRecordDto>> Confirm(Guid workRecordId, ConfirmWorkRecordRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new ConfirmWorkRecordCommand(workRecordId, request.ExpectedVersion), cancellationToken));

    [HttpPost("{workRecordId:guid}/corrections")]
    public async Task<ActionResult<WorkRecordDto>> Correct(Guid workRecordId, CorrectWorkRecordRequest request, CancellationToken cancellationToken) =>
        Ok(await sender.Send(new CorrectWorkRecordCommand(workRecordId, request.ExpectedVersion, request.CorrectionReason,
            request.PayBasis, request.ActivityIds, request.Quantity, Scope(request.Scope), request.LateEntryReason), cancellationToken));

    private static WorkScopeCommand? Scope(WorkScopeRequest? scope) => scope is null
        ? null
        : new WorkScopeCommand(scope.Type, scope.StartLine, scope.EndLine, scope.SectionName);

    private static ValidationProblemDetails DateError(string propertyName) => new(
        new Dictionary<string, string[]> { [propertyName] = ["Date must use yyyy-MM-dd."] });
}
