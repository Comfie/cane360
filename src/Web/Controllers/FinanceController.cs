using Cane360.Application.Common.Interfaces;
using Cane360.Application.Finance;
using Cane360.Web.Infrastructure;
using Cane360.Web.Models.Finance;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Cane360.Web.Controllers;

[Authorize]
[ApiController]
[Route("api/finance")]
public sealed class FinanceController(IFinanceService finance) : ControllerBase
{
    [HttpGet("session", Name = "GetFinanceSession")]
    public async Task<ActionResult<FinanceSessionDto>> GetSession(CancellationToken cancellationToken) =>
        Ok(await finance.GetSessionAsync(cancellationToken));

    [HttpGet("transactions", Name = "GetFinanceTransactions")]
    public async Task<ActionResult<IReadOnlyList<OperationalTransactionDto>>> GetTransactions(
        [FromQuery] string? from, [FromQuery] string? to, [FromQuery] string? type,
        [FromQuery] string? category, [FromQuery] string? status, [FromQuery] string? search,
        CancellationToken cancellationToken)
    {
        if (!TryOptionalDate(from, out DateOnly? fromDate) || !TryOptionalDate(to, out DateOnly? toDate))
            return BadRequest(new ValidationProblemDetails(new Dictionary<string, string[]>
                { ["date"] = ["Dates must use yyyy-MM-dd."] }));
        return Ok(await finance.GetTransactionsAsync(new(fromDate, toDate, type, category, status,
            search), cancellationToken));
    }

    [HttpGet("transactions/{transactionId:guid}", Name = "GetFinanceTransaction")]
    public async Task<ActionResult<OperationalTransactionDto>> GetTransaction(Guid transactionId,
        CancellationToken cancellationToken) => Ok(await finance.GetTransactionAsync(transactionId,
            cancellationToken));

    [HttpPost("transactions", Name = "CreateFinanceTransaction")]
    public async Task<ActionResult<OperationalTransactionDto>> CreateTransaction(
        CreateOperationalTransactionRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.EventDate, out DateOnly eventDate))
            return DateError(nameof(request.EventDate));
        OperationalTransactionDto result = await finance.CreateAsync(new(request.Type,
            request.Category, eventDate, request.PayeeOrPayer, request.AmountUsd,
            request.SourceReference, request.Notes), cancellationToken);
        return CreatedAtAction(nameof(GetTransaction), new { transactionId = result.Id }, result);
    }

    [HttpPut("transactions/{transactionId:guid}", Name = "UpdateFinanceTransaction")]
    public async Task<ActionResult<OperationalTransactionDto>> UpdateTransaction(Guid transactionId,
        UpdateOperationalTransactionRequest request, CancellationToken cancellationToken)
    {
        if (!TransportValueParser.TryParseDateOnly(request.EventDate, out DateOnly eventDate))
            return DateError(nameof(request.EventDate));
        return Ok(await finance.UpdateAsync(transactionId, new(request.Type, request.Category,
            eventDate, request.PayeeOrPayer, request.AmountUsd, request.SourceReference,
            request.Notes, request.ExpectedVersion), cancellationToken));
    }

    [HttpPut("transactions/{transactionId:guid}/allocations", Name = "SetFinanceTransactionAllocations")]
    public async Task<ActionResult<OperationalTransactionDto>> SetAllocations(Guid transactionId,
        SetTransactionAllocationsRequest request, CancellationToken cancellationToken) => Ok(await
        finance.SetAllocationsAsync(transactionId, new(request.ExpectedVersion,
            request.Allocations.Select(x => new TransactionAllocationInput(x.CropCycleId, x.FieldId,
                x.Category, x.AmountUsd, x.AllocationType)).ToArray()), cancellationToken));

    [HttpPost("transactions/{transactionId:guid}/post", Name = "PostFinanceTransaction")]
    public async Task<ActionResult<OperationalTransactionDto>> PostTransaction(Guid transactionId,
        PostOperationalTransactionRequest request, CancellationToken cancellationToken) => Ok(await
        finance.PostAsync(transactionId, new(request.ExpectedVersion, request.IdempotencyKey,
            request.AuthorizedClosedCycleCorrection, request.CorrectionReason), cancellationToken));

    [HttpPost("transactions/{transactionId:guid}/reverse", Name = "ReverseFinanceTransaction")]
    public async Task<ActionResult<OperationalTransactionDto>> ReverseTransaction(Guid transactionId,
        ReverseOperationalTransactionRequest request, CancellationToken cancellationToken) => Ok(await
        finance.ReverseAsync(transactionId, new(request.Reason, request.IdempotencyKey), cancellationToken));

    [HttpGet("crop-cycles/{cropCycleId:guid}/cost", Name = "GetFinanceCropCycleCost")]
    public async Task<ActionResult<CropCycleCostSummaryDto>> GetCropCycleCost(Guid cropCycleId,
        CancellationToken cancellationToken) => Ok(await finance.GetCropCycleCostAsync(cropCycleId,
            cancellationToken));

    [HttpPost("payroll-costs/reconcile", Name = "ReconcileFinancePayrollCosts")]
    public async Task<ActionResult<PayrollCostReconciliationDto>> ReconcilePayrollCosts(
        CancellationToken cancellationToken) => Ok(await finance.ReconcilePayrollAsync(cancellationToken));

    [HttpGet("crop-cycles/{cropCycleId:guid}/budgets", Name = "GetFinanceBudgets")]
    public async Task<ActionResult<IReadOnlyList<BudgetDto>>> GetBudgets(Guid cropCycleId,
        CancellationToken cancellationToken) => Ok(await finance.GetBudgetsAsync(cropCycleId,
            cancellationToken));

    [HttpGet("budgets/{budgetId:guid}", Name = "GetFinanceBudget")]
    public async Task<ActionResult<BudgetDto>> GetBudget(Guid budgetId,
        CancellationToken cancellationToken) => Ok(await finance.GetBudgetAsync(budgetId,
            cancellationToken));

    [HttpGet("crop-cycles/{cropCycleId:guid}/budgets/current", Name = "GetCurrentFinanceBudget")]
    public async Task<ActionResult<BudgetDto>> GetCurrentBudget(Guid cropCycleId,
        CancellationToken cancellationToken)
    {
        BudgetDto? result = await finance.GetCurrentApprovedBudgetAsync(cropCycleId, cancellationToken);
        return result is null ? NotFound() : Ok(result);
    }

    [HttpPost("budgets", Name = "CreateFinanceBudget")]
    public async Task<ActionResult<BudgetDto>> CreateBudget(CreateBudgetRequest request,
        CancellationToken cancellationToken)
    {
        BudgetDto result = await finance.CreateBudgetAsync(new(request.CropCycleId, request.Name,
            request.ReportingAreaHa, request.ExpectedProductionTonnes, request.Notes), cancellationToken);
        return CreatedAtAction(nameof(GetBudget), new { budgetId = result.Id }, result);
    }

    [HttpPut("budgets/{budgetId:guid}", Name = "UpdateFinanceBudget")]
    public async Task<ActionResult<BudgetDto>> UpdateBudget(Guid budgetId,
        UpdateBudgetRequest request, CancellationToken cancellationToken) => Ok(await
        finance.UpdateBudgetAsync(budgetId, new(request.Name, request.ReportingAreaHa,
            request.ExpectedProductionTonnes, request.Notes, request.ExpectedRowVersion),
            cancellationToken));

    [HttpPost("budgets/{budgetId:guid}/lines", Name = "AddFinanceBudgetLine")]
    public async Task<ActionResult<BudgetDto>> AddBudgetLine(Guid budgetId,
        BudgetLineRequest request, CancellationToken cancellationToken) => Ok(await
        finance.AddBudgetLineAsync(budgetId, LineInput(request), cancellationToken));

    [HttpPut("budgets/{budgetId:guid}/lines/{lineId:guid}", Name = "UpdateFinanceBudgetLine")]
    public async Task<ActionResult<BudgetDto>> UpdateBudgetLine(Guid budgetId, Guid lineId,
        BudgetLineRequest request, CancellationToken cancellationToken) => Ok(await
        finance.UpdateBudgetLineAsync(budgetId, lineId, LineInput(request), cancellationToken));

    [HttpDelete("budgets/{budgetId:guid}/lines/{lineId:guid}", Name = "RemoveFinanceBudgetLine")]
    public async Task<ActionResult<BudgetDto>> RemoveBudgetLine(Guid budgetId, Guid lineId,
        [FromQuery] long expectedRowVersion, CancellationToken cancellationToken) => Ok(await
        finance.RemoveBudgetLineAsync(budgetId, lineId, new(expectedRowVersion), cancellationToken));

    [HttpPost("budgets/{budgetId:guid}/submit", Name = "SubmitFinanceBudget")]
    public async Task<ActionResult<BudgetDto>> SubmitBudget(Guid budgetId,
        BudgetActionRequest request, CancellationToken cancellationToken) => Ok(await
        finance.SubmitBudgetAsync(budgetId, new(request.ExpectedRowVersion), cancellationToken));

    [HttpPost("budgets/{budgetId:guid}/approve", Name = "ApproveFinanceBudget")]
    public async Task<ActionResult<BudgetDto>> ApproveBudget(Guid budgetId,
        ApproveBudgetRequest request, CancellationToken cancellationToken) => Ok(await
        finance.ApproveBudgetAsync(budgetId, new(request.ExpectedRowVersion,
            request.IdempotencyKey), cancellationToken));

    [HttpPost("budgets/{budgetId:guid}/revisions", Name = "CreateFinanceBudgetRevision")]
    public async Task<ActionResult<BudgetDto>> CreateBudgetRevision(Guid budgetId,
        CreateBudgetRevisionRequest request, CancellationToken cancellationToken) => Ok(await
        finance.CreateBudgetRevisionAsync(budgetId, new(request.Name, request.Notes), cancellationToken));

    [HttpGet("crop-cycles/{cropCycleId:guid}/budget-variance", Name = "GetFinanceBudgetVariance")]
    public async Task<ActionResult<BudgetVarianceReportDto>> GetBudgetVariance(Guid cropCycleId,
        CancellationToken cancellationToken) => Ok(await finance.GetBudgetVarianceAsync(cropCycleId,
            cancellationToken));

    private static bool TryOptionalDate(string? value, out DateOnly? date)
    {
        date = null;
        if (string.IsNullOrWhiteSpace(value)) return true;
        if (!TransportValueParser.TryParseDateOnly(value, out DateOnly parsed)) return false;
        date = parsed;
        return true;
    }

    private BadRequestObjectResult DateError(string propertyName) => BadRequest(
        new ValidationProblemDetails(new Dictionary<string, string[]>
            { [propertyName] = ["Date must use yyyy-MM-dd."] }));

    private static BudgetLineInput LineInput(BudgetLineRequest request) => new(request.Category,
        request.Description, request.AmountUsd, request.Quantity, request.Unit,
        request.UnitRateUsd, request.Notes, request.ExpectedRowVersion);
}
