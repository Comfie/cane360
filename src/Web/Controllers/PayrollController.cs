using Cane360.Application.Payroll;
using Cane360.Application.Common.Interfaces;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Payroll;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/payroll")]
public sealed class PayrollController(ISender sender, IPayrollSettlementService? settlement = null) : ControllerBase
{
    private IPayrollSettlementService SettlementService => settlement ?? throw new InvalidOperationException("Payroll settlement service is unavailable.");
    [HttpGet("workspace")]
    [EndpointSummary("Get payroll workspace")]
    [EndpointDescription("Returns payroll workspace for the authenticated farm.")]
    [ProducesResponseType<PayrollWorkspaceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<PayrollWorkspaceDto>> Workspace(CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollWorkspaceQuery(), cancellationToken));
    [HttpGet("periods", Name = "GetPeriodsPayroll")]
    [EndpointSummary("List payroll periods")]
    [EndpointDescription("Returns payroll periods for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<PayrollPeriodDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PayrollPeriodDto>>> Periods(CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollPeriodsQuery(), cancellationToken));
    [HttpPost("periods", Name = "CreatePeriodPayroll")]
    [EndpointSummary("Create payroll period")]
    [EndpointDescription("Creates payroll period for the authenticated farm.")]
    [ProducesResponseType<PayrollPeriodDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollPeriodDto>> CreatePeriod(CreatePayrollPeriodRequest request, CancellationToken cancellationToken) { var result = await sender.Send(new CreatePayrollPeriodCommand(request.Year, request.Month), cancellationToken); return Ok(result); }
    [HttpPost("periods/{periodId:guid}/open")]
    [EndpointSummary("Open payroll period")]
    [EndpointDescription("Opens payroll period for the authenticated farm.")]
    [ProducesResponseType<PayrollPeriodDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollPeriodDto>> OpenPeriod(Guid periodId, VersionedPayrollRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new OpenPayrollPeriodCommand(periodId, request.ExpectedVersion), cancellationToken));
    [HttpPost("periods/{periodId:guid}/cancel", Name = "CancelPeriodPayroll")]
    [EndpointSummary("Cancel payroll period")]
    [EndpointDescription("Cancels payroll period for the authenticated farm.")]
    [ProducesResponseType<PayrollPeriodDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollPeriodDto>> CancelPeriod(Guid periodId, CancelPayrollPeriodRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new CancelPayrollPeriodCommand(periodId, request.ExpectedVersion, request.Reason), cancellationToken));
    [HttpGet("periods/{periodId:guid}/preflight")]
    [EndpointSummary("Get payroll preflight")]
    [EndpointDescription("Returns payroll preflight for the authenticated farm.")]
    [ProducesResponseType<PayrollPreflightDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollPreflightDto>> Preflight(Guid periodId, [FromQuery] Guid? workerId, [FromQuery] bool? eligible, [FromQuery] string? evidenceType, [FromQuery] int page = 1, [FromQuery] int pageSize = 25, CancellationToken cancellationToken = default) => Ok(await sender.Send(new GetPayrollPreflightQuery(periodId, workerId, eligible, evidenceType, page, pageSize), cancellationToken));
    [HttpGet("advances", Name = "GetAdvancesPayroll")]
    [EndpointSummary("List worker advances")]
    [EndpointDescription("Returns worker advances for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<WorkerAdvanceDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<WorkerAdvanceDto>>> Advances(CancellationToken cancellationToken) => Ok(await sender.Send(new GetWorkerAdvancesQuery(), cancellationToken));
    [HttpGet("advances/{advanceId:guid}", Name = "GetWorkerAdvance")]
    [EndpointSummary("Get worker advance")]
    [EndpointDescription("Returns worker advance for the authenticated farm.")]
    [ProducesResponseType<WorkerAdvanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkerAdvanceDto>> Advance(Guid advanceId, CancellationToken cancellationToken) => Ok(await sender.Send(new GetWorkerAdvanceQuery(advanceId), cancellationToken));
    [HttpPost("advances/schedule-preview")]
    [EndpointSummary("Preview advance schedule")]
    [EndpointDescription("Previews advance schedule for the authenticated farm.")]
    [ProducesResponseType<AdvanceSchedulePreviewDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<AdvanceSchedulePreviewDto>> PreviewAdvanceSchedule(PreviewAdvanceScheduleRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new PreviewAdvanceScheduleQuery(request.AmountUsd, request.RecoveryStartPayrollPeriodId, request.InstallmentCount), cancellationToken));
    [HttpPost("advances", Name = "CreateAdvancePayroll")]
    [EndpointSummary("Create worker advance")]
    [EndpointDescription("Creates worker advance for the authenticated farm.")]
    [ProducesResponseType<WorkerAdvanceDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkerAdvanceDto>> CreateAdvance(CreateWorkerAdvanceRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.RequestedEventDate, out var eventDate)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.RequestedEventDate)] = ["Date must use yyyy-MM-dd."] }));
        var result = await sender.Send(new CreateWorkerAdvanceCommand(request.WorkerId, request.AmountUsd, request.Reason, eventDate, request.RecoveryStartPayrollPeriodId, request.InstallmentCount, request.InstallmentPeriodIds ?? []), cancellationToken); return CreatedAtAction(nameof(Advance), new { advanceId = result.Id }, result);
    }
    [HttpPut("advances/{advanceId:guid}", Name = "UpdateWorkerAdvance")]
    [EndpointSummary("Update worker advance")]
    [EndpointDescription("Updates worker advance for the authenticated farm.")]
    [ProducesResponseType<WorkerAdvanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkerAdvanceDto>> UpdateAdvance(Guid advanceId, UpdateWorkerAdvanceRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.RequestedEventDate, out var eventDate)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.RequestedEventDate)] = ["Date must use yyyy-MM-dd."] }));
        return Ok(await sender.Send(new UpdateWorkerAdvanceCommand(advanceId, request.AmountUsd, request.Reason, eventDate, request.RecoveryStartPayrollPeriodId, request.InstallmentCount, request.ExpectedVersion), cancellationToken));
    }
    [HttpPost("advances/{advanceId:guid}/cancel", Name = "CancelAdvancePayroll")]
    [EndpointSummary("Cancel worker advance")]
    [EndpointDescription("Cancels worker advance for the authenticated farm.")]
    [ProducesResponseType<WorkerAdvanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkerAdvanceDto>> CancelAdvance(Guid advanceId, CancelWorkerAdvanceRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new CancelWorkerAdvanceCommand(advanceId, request.ExpectedVersion, request.Reason), cancellationToken));
    [HttpPost("advances/{advanceId:guid}/submit", Name = "SubmitAdvancePayroll")]
    [EndpointSummary("Submit worker advance")]
    [EndpointDescription("Submits worker advance for the authenticated farm.")]
    [ProducesResponseType<WorkerAdvanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkerAdvanceDto>> SubmitAdvance(Guid advanceId, VersionedPayrollRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new SubmitWorkerAdvanceCommand(advanceId, request.ExpectedVersion), cancellationToken));
    [HttpPost("advances/{advanceId:guid}/decision", Name = "DecideAdvancePayroll")]
    [EndpointSummary("Decide worker advance")]
    [EndpointDescription("Records a decision for worker advance for the authenticated farm.")]
    [ProducesResponseType<WorkerAdvanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkerAdvanceDto>> DecideAdvance(Guid advanceId, DecideWorkerAdvanceRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new DecideWorkerAdvanceCommand(advanceId, request.ExpectedVersion, request.Approved, request.Reason, request.IdempotencyKey), cancellationToken));
    [HttpPost("advances/{advanceId:guid}/issue")]
    [EndpointSummary("Issue worker advance")]
    [EndpointDescription("Issues worker advance for the authenticated farm.")]
    [ProducesResponseType<WorkerAdvanceDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<WorkerAdvanceDto>> IssueAdvance(Guid advanceId, IssueWorkerAdvanceRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new IssueWorkerAdvanceCommand(advanceId, request.ExpectedVersion, request.PaymentMethod, request.AmountUsd, request.IssuedAt, request.PayingPersonId, request.WorkerAcknowledged, request.Provider, request.RecipientNumber, request.ExternalReference, request.TransactionStatus, request.IdempotencyKey), cancellationToken));
    [HttpGet("runs", Name = "GetRunsPayroll")]
    [EndpointSummary("List payroll runs")]
    [EndpointDescription("Returns payroll runs for the authenticated farm.")]
    [ProducesResponseType<IReadOnlyList<PayrollRunDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<IReadOnlyList<PayrollRunDto>>> Runs(CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollRunsQuery(), cancellationToken));
    [HttpGet("runs/{runId:guid}")]
    [EndpointSummary("Get payroll run")]
    [EndpointDescription("Returns payroll run for the authenticated farm.")]
    [ProducesResponseType<PayrollRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollRunDto>> Run(Guid runId, CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollRunQuery(runId), cancellationToken));
    [HttpPost("runs", Name = "CreateRunPayroll")]
    [EndpointSummary("Create payroll run")]
    [EndpointDescription("Creates payroll run for the authenticated farm.")]
    [ProducesResponseType<PayrollRunDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollRunDto>> CreateRun(CreatePayrollRunRequest request, CancellationToken cancellationToken) { var result = await sender.Send(new CreatePayrollRunCommand(request.PayrollPeriodId), cancellationToken); return CreatedAtAction(nameof(Run), new { runId = result.Id }, result); }
    [HttpPost("runs/{runId:guid}/calculate")]
    [EndpointSummary("Calculate payroll run")]
    [EndpointDescription("Calculates payroll run for the authenticated farm.")]
    [ProducesResponseType<PayrollRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollRunDto>> CalculateRun(Guid runId, VersionedPayrollRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new CalculatePayrollRunCommand(runId, request.ExpectedVersion), cancellationToken));
    [HttpGet("runs/{runId:guid}/calculations/{calculationVersion:int}")]
    [EndpointSummary("Get payroll calculation")]
    [EndpointDescription("Returns payroll calculation for the authenticated farm.")]
    [ProducesResponseType<PayrollCalculationDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollCalculationDto>> Calculation(Guid runId, int calculationVersion, CancellationToken cancellationToken) => Ok(await sender.Send(new GetPayrollCalculationQuery(runId, calculationVersion), cancellationToken));
    [HttpGet("runs/{runId:guid}/calculations/{calculationVersion:int}/worker-lines/{workerId:guid}", Name = "GetPayrollWorkerLine")]
    [EndpointSummary("Get payroll worker line")]
    [EndpointDescription("Returns payroll worker line for the authenticated farm.")]
    [ProducesResponseType<PayrollWorkerLineDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollWorkerLineDto>> WorkerLine(Guid runId, int calculationVersion, Guid workerId, CancellationToken cancellationToken) { var result = await sender.Send(new GetPayrollCalculationQuery(runId, calculationVersion), cancellationToken); var line = result.Workers.SingleOrDefault(x => x.WorkerId == workerId); return line is null ? NotFound() : Ok(line); }
    [HttpPost("runs/{runId:guid}/submit", Name = "SubmitRunPayroll")]
    [EndpointSummary("Submit payroll run")]
    [EndpointDescription("Submits payroll run for the authenticated farm.")]
    [ProducesResponseType<PayrollRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollRunDto>> SubmitRun(Guid runId, SubmitPayrollRunRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new SubmitPayrollRunCommand(runId, request.ExpectedVersion, request.CalculationVersion), cancellationToken));
    [HttpPost("runs/{runId:guid}/decision", Name = "DecideRunPayroll")]
    [EndpointSummary("Decide payroll run")]
    [EndpointDescription("Records a decision for payroll run for the authenticated farm.")]
    [ProducesResponseType<PayrollRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollRunDto>> DecideRun(Guid runId, DecidePayrollRunRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new DecidePayrollRunCommand(runId, request.ExpectedVersion, request.CalculationVersion, request.Approved, request.Reason, request.IdempotencyKey), cancellationToken));
    [HttpPost("runs/{runId:guid}/cancel", Name = "CancelRunPayroll")]
    [EndpointSummary("Cancel payroll run")]
    [EndpointDescription("Cancels payroll run for the authenticated farm.")]
    [ProducesResponseType<PayrollRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollRunDto>> CancelRun(Guid runId, CancelPayrollRunRequest request, CancellationToken cancellationToken) => Ok(await sender.Send(new CancelPayrollRunCommand(runId, request.ExpectedVersion, request.Reason), cancellationToken));
    [HttpGet("runs/{runId:guid}/approved-source-chain")]
    [EndpointSummary("Get approved payroll source chain")]
    [EndpointDescription("Returns approved payroll source chain for the authenticated farm.")]
    [ProducesResponseType<PayrollRunDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PayrollRunDto>> ApprovedSourceChain(Guid runId, CancellationToken cancellationToken) { var result = await sender.Send(new GetPayrollRunQuery(runId), cancellationToken); return result.Status != "Approved" ? Conflict() : Ok(result); }

    [HttpGet("runs/{runId:guid}/settlement")]
    [EndpointSummary("Get payroll settlement")]
    [EndpointDescription("Returns payroll settlement for the authenticated farm.")]
    [ProducesResponseType<RunSettlementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RunSettlementDto>> Settlement(Guid runId, CancellationToken cancellationToken) => Ok(await SettlementService.GetRunAsync(runId, cancellationToken));

    [HttpGet("runs/{runId:guid}/settlement/workers/{workerLineId:guid}")]
    [EndpointSummary("Get worker settlement")]
    [EndpointDescription("Returns worker settlement for the authenticated farm.")]
    [ProducesResponseType<WorkerSettlementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<WorkerSettlementDto>> WorkerSettlement(Guid runId, Guid workerLineId, [FromQuery] int calculationVersion, CancellationToken cancellationToken) => Ok(await SettlementService.GetWorkerAsync(runId, calculationVersion, workerLineId, cancellationToken));

    [HttpPost("runs/{runId:guid}/payments")]
    [EndpointSummary("Record payroll payment")]
    [EndpointDescription("Records payroll payment for the authenticated farm.")]
    [ProducesResponseType<PayrollPaymentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollPaymentDto>> RecordPayment(Guid runId, RecordPayrollPaymentRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.PaymentDate, out var paymentDate)) return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]> { [nameof(request.PaymentDate)] = ["Date must use yyyy-MM-dd."] }));
        var result = await SettlementService.RecordPaymentAsync(runId, new(request.CalculationVersion, request.PayrollWorkerLineId, request.Method, request.AmountUsd, paymentDate, request.Provider, request.RecipientNumber, request.TransactionReference, request.ExternalStatus, request.IdempotencyKey), cancellationToken);
        return Ok(result);
    }

    [HttpPost("payments/{paymentId:guid}/acknowledgement")]
    [EndpointSummary("Acknowledge payroll payment")]
    [EndpointDescription("Acknowledges payroll payment for the authenticated farm.")]
    [ProducesResponseType<PayrollPaymentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollPaymentDto>> AcknowledgePayment(Guid paymentId, RecordPaymentAcknowledgementRequest request, CancellationToken cancellationToken) => Ok(await SettlementService.AcknowledgeAsync(paymentId, new(request.Status, request.AcknowledgedByPersonId, request.AcknowledgedAt, request.EvidenceReference, request.IdempotencyKey), cancellationToken));

    [HttpPost("payments/{paymentId:guid}/reversal")]
    [EndpointSummary("Reverse payroll payment")]
    [EndpointDescription("Reverses payroll payment for the authenticated farm.")]
    [ProducesResponseType<PayrollPaymentDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PayrollPaymentDto>> ReversePayment(Guid paymentId, ReversePayrollPaymentRequest request, CancellationToken cancellationToken) => Ok(await SettlementService.ReverseAsync(paymentId, new(request.AmountUsd, request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpPost("runs/{runId:guid}/settlement/close", Name = "CloseSettlementPayroll")]
    [EndpointSummary("Close payroll settlement")]
    [EndpointDescription("Closes payroll settlement for the authenticated farm.")]
    [ProducesResponseType<RunSettlementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RunSettlementDto>> CloseSettlement(Guid runId, ClosePayrollSettlementRequest request, CancellationToken cancellationToken) => Ok(await SettlementService.CloseAsync(runId, new(request.CalculationVersion, request.IdempotencyKey), cancellationToken));

    [HttpPost("runs/{runId:guid}/settlement/reopen")]
    [EndpointSummary("Reopen payroll settlement")]
    [EndpointDescription("Reopens payroll settlement for the authenticated farm.")]
    [ProducesResponseType<RunSettlementDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RunSettlementDto>> ReopenSettlement(Guid runId, ReopenPayrollSettlementRequest request, CancellationToken cancellationToken) => Ok(await SettlementService.ReopenAsync(runId, new(request.CalculationVersion, request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpGet("runs/{runId:guid}/calculations/{calculationVersion:int}/worker-lines/{workerLineId:guid}/payslip")]
    [EndpointSummary("Get operational payslip")]
    [EndpointDescription("Returns operational payslip for the authenticated farm.")]
    [ProducesResponseType<OperationalPayslipDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<OperationalPayslipDto>> Payslip(Guid runId, int calculationVersion, Guid workerLineId, CancellationToken cancellationToken) => Ok(await SettlementService.GetPayslipAsync(runId, calculationVersion, workerLineId, cancellationToken));

    [HttpGet("runs/{runId:guid}/calculations/{calculationVersion:int}/cash-register")]
    [EndpointSummary("Get cash payment register")]
    [EndpointDescription("Returns cash payment register for the authenticated farm.")]
    [ProducesResponseType<CashPaymentRegisterDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CashPaymentRegisterDto>> CashRegister(Guid runId, int calculationVersion, CancellationToken cancellationToken) => Ok(await SettlementService.GetCashRegisterAsync(runId, calculationVersion, cancellationToken));
}
