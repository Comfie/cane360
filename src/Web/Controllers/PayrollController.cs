using Cane360.Application.Payroll;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Payroll;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/payroll")]
public sealed class PayrollController(ISender sender) : ControllerBase
{
    [HttpGet("workspace")]
    public async Task<ActionResult<PayrollWorkspaceDto>> Workspace(CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollWorkspaceQuery(), cancellationToken));
    [HttpGet("periods")]
    public async Task<ActionResult<IReadOnlyList<PayrollPeriodDto>>> Periods(CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollPeriodsQuery(), cancellationToken));
    [HttpPost("periods")]
    public async Task<ActionResult<PayrollPeriodDto>> CreatePeriod(CreatePayrollPeriodRequest request, CancellationToken cancellationToken) { var result = await sender.Send(new CreatePayrollPeriodCommand(request.Year, request.Month), cancellationToken); return CreatedAtAction(nameof(Periods), result); }
    [HttpPost("periods/{periodId:guid}/open")]
    public async Task<ActionResult<PayrollPeriodDto>> OpenPeriod(Guid periodId, VersionedPayrollRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new OpenPayrollPeriodCommand(periodId, request.ExpectedVersion), cancellationToken));
    [HttpPost("periods/{periodId:guid}/cancel")]
    public async Task<ActionResult<PayrollPeriodDto>> CancelPeriod(Guid periodId, CancelPayrollPeriodRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new CancelPayrollPeriodCommand(periodId, request.ExpectedVersion, request.Reason), cancellationToken));
    [HttpGet("periods/{periodId:guid}/preflight")]
    public async Task<ActionResult<PayrollPreflightDto>> Preflight(Guid periodId, [FromQuery] Guid? workerId, [FromQuery] bool? eligible, [FromQuery] string? evidenceType, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) => Ok(await sender.Send(new GetPayrollPreflightQuery(periodId, workerId, eligible, evidenceType, page, pageSize), cancellationToken));
    [HttpGet("advances")]
    public async Task<ActionResult<IReadOnlyList<WorkerAdvanceDto>>> Advances(CancellationToken cancellationToken) => Ok(await sender.Send(new GetWorkerAdvancesQuery(), cancellationToken));
    [HttpGet("advances/{advanceId:guid}")]
    public async Task<ActionResult<WorkerAdvanceDto>> Advance(Guid advanceId, CancellationToken cancellationToken) => Ok(await sender.Send(new GetWorkerAdvanceQuery(advanceId), cancellationToken));
    [HttpPost("advances/schedule-preview")]
    public async Task<ActionResult<AdvanceSchedulePreviewDto>> PreviewAdvanceSchedule(PreviewAdvanceScheduleRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new PreviewAdvanceScheduleQuery(request.AmountUsd, request.RecoveryStartPayrollPeriodId, request.InstallmentCount), cancellationToken));
    [HttpPost("advances")]
    public async Task<ActionResult<WorkerAdvanceDto>> CreateAdvance(CreateWorkerAdvanceRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.RequestedEventDate, out var eventDate)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.RequestedEventDate)] = ["Date must use yyyy-MM-dd."] }));
        var result = await sender.Send(new CreateWorkerAdvanceCommand(request.WorkerId, request.AmountUsd, request.Reason, eventDate, request.RecoveryStartPayrollPeriodId, request.InstallmentCount, request.InstallmentPeriodIds ?? []), cancellationToken); return CreatedAtAction(nameof(Advances), result);
    }
    [HttpPut("advances/{advanceId:guid}")]
    public async Task<ActionResult<WorkerAdvanceDto>> UpdateAdvance(Guid advanceId, UpdateWorkerAdvanceRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.RequestedEventDate, out var eventDate)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.RequestedEventDate)] = ["Date must use yyyy-MM-dd."] }));
        return Ok(await sender.Send(new UpdateWorkerAdvanceCommand(advanceId, request.AmountUsd, request.Reason, eventDate, request.RecoveryStartPayrollPeriodId, request.InstallmentCount, request.ExpectedVersion), cancellationToken));
    }
    [HttpPost("advances/{advanceId:guid}/cancel")]
    public async Task<ActionResult<WorkerAdvanceDto>> CancelAdvance(Guid advanceId, CancelWorkerAdvanceRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new CancelWorkerAdvanceCommand(advanceId, request.ExpectedVersion, request.Reason), cancellationToken));
    [HttpPost("advances/{advanceId:guid}/submit")]
    public async Task<ActionResult<WorkerAdvanceDto>> SubmitAdvance(Guid advanceId, VersionedPayrollRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new SubmitWorkerAdvanceCommand(advanceId, request.ExpectedVersion), cancellationToken));
    [HttpPost("advances/{advanceId:guid}/decision")]
    public async Task<ActionResult<WorkerAdvanceDto>> DecideAdvance(Guid advanceId, DecideWorkerAdvanceRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new DecideWorkerAdvanceCommand(advanceId, request.ExpectedVersion, request.Approved, request.Reason, request.IdempotencyKey), cancellationToken));
    [HttpPost("advances/{advanceId:guid}/issue")]
    public async Task<ActionResult<WorkerAdvanceDto>> IssueAdvance(Guid advanceId, IssueWorkerAdvanceRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new IssueWorkerAdvanceCommand(advanceId, request.ExpectedVersion, request.PaymentMethod, request.AmountUsd, request.IssuedAt, request.PayingPersonId, request.WorkerAcknowledged, request.Provider, request.RecipientNumber, request.ExternalReference, request.TransactionStatus, request.IdempotencyKey), cancellationToken));
    [HttpGet("runs")]
    public async Task<ActionResult<IReadOnlyList<PayrollRunDto>>> Runs(CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollRunsQuery(), cancellationToken));
    [HttpGet("runs/{runId:guid}")]
    public async Task<ActionResult<PayrollRunDto>> Run(Guid runId, CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollRunQuery(runId), cancellationToken));
    [HttpPost("runs")]
    public async Task<ActionResult<PayrollRunDto>> CreateRun(CreatePayrollRunRequest request, CancellationToken cancellationToken) { var result = await sender.Send(new CreatePayrollRunCommand(request.PayrollPeriodId), cancellationToken); return CreatedAtAction(nameof(Run), new { runId = result.Id }, result); }
    [HttpPost("runs/{runId:guid}/calculate")]
    public async Task<ActionResult<PayrollRunDto>> CalculateRun(Guid runId, VersionedPayrollRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new CalculatePayrollRunCommand(runId, request.ExpectedVersion), cancellationToken));
    [HttpGet("runs/{runId:guid}/calculations/{calculationVersion:int}")]
    public async Task<ActionResult<PayrollCalculationDto>> Calculation(Guid runId, int calculationVersion, CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollCalculationQuery(runId, calculationVersion), cancellationToken));
    [HttpGet("runs/{runId:guid}/calculations/{calculationVersion:int}/worker-lines/{workerId:guid}")]
    public async Task<ActionResult<PayrollWorkerLineDto>> WorkerLine(Guid runId, int calculationVersion, Guid workerId, CancellationToken cancellationToken) { var result = await sender.Send(new GetPayrollCalculationQuery(runId, calculationVersion), cancellationToken); var line = result.Workers.SingleOrDefault(x => x.WorkerId == workerId); return line is null ? NotFound() : Ok(line); }
    [HttpPost("runs/{runId:guid}/submit")]
    public async Task<ActionResult<PayrollRunDto>> SubmitRun(Guid runId, SubmitPayrollRunRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new SubmitPayrollRunCommand(runId, request.ExpectedVersion, request.CalculationVersion), cancellationToken));
    [HttpPost("runs/{runId:guid}/decision")]
    public async Task<ActionResult<PayrollRunDto>> DecideRun(Guid runId, DecidePayrollRunRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new DecidePayrollRunCommand(runId, request.ExpectedVersion, request.CalculationVersion, request.Approved, request.Reason, request.IdempotencyKey), cancellationToken));
    [HttpPost("runs/{runId:guid}/cancel")]
    public async Task<ActionResult<PayrollRunDto>> CancelRun(Guid runId, CancelPayrollRunRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new CancelPayrollRunCommand(runId, request.ExpectedVersion, request.Reason), cancellationToken));
    [HttpGet("runs/{runId:guid}/approved-source-chain")]
    public async Task<ActionResult<PayrollRunDto>> ApprovedSourceChain(Guid runId, CancellationToken cancellationToken) { var result = await sender.Send(new GetPayrollRunQuery(runId), cancellationToken); return result.Status != "Approved" ? Conflict() : Ok(result); }
}
