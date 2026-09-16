using Cane360.Application.Finance;

namespace Cane360.Application.Common.Interfaces;

public interface IFinanceService
{
    Task<FinanceSessionDto> GetSessionAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OperationalTransactionDto>> GetTransactionsAsync(FinanceTransactionFilter filter, CancellationToken cancellationToken);
    Task<OperationalTransactionPageDto> GetTransactionPageAsync(FinanceTransactionFilter filter,
        int page, int pageSize, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> CreateAsync(CreateOperationalTransactionInput input, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> UpdateAsync(Guid transactionId, UpdateOperationalTransactionInput input, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> SetAllocationsAsync(Guid transactionId, SetTransactionAllocationsInput input, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> PostAsync(Guid transactionId, PostOperationalTransactionInput input, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> ReverseAsync(Guid transactionId, ReverseOperationalTransactionInput input, CancellationToken cancellationToken);
    Task<CropCycleCostSummaryDto> GetCropCycleCostAsync(Guid cropCycleId, CancellationToken cancellationToken);
    Task<PayrollCostReconciliationDto> ReconcilePayrollAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<BudgetDto>> GetBudgetsAsync(Guid cropCycleId, CancellationToken cancellationToken);
    Task<BudgetDto> GetBudgetAsync(Guid budgetId, CancellationToken cancellationToken);
    Task<BudgetDto?> GetCurrentApprovedBudgetAsync(Guid cropCycleId, CancellationToken cancellationToken);
    Task<BudgetDto> CreateBudgetAsync(CreateBudgetInput input, CancellationToken cancellationToken);
    Task<BudgetDto> UpdateBudgetAsync(Guid budgetId, UpdateBudgetInput input, CancellationToken cancellationToken);
    Task<BudgetDto> AddBudgetLineAsync(Guid budgetId, BudgetLineInput input, CancellationToken cancellationToken);
    Task<BudgetDto> UpdateBudgetLineAsync(Guid budgetId, Guid lineId, BudgetLineInput input, CancellationToken cancellationToken);
    Task<BudgetDto> RemoveBudgetLineAsync(Guid budgetId, Guid lineId, BudgetActionInput input, CancellationToken cancellationToken);
    Task<BudgetDto> SubmitBudgetAsync(Guid budgetId, BudgetActionInput input, CancellationToken cancellationToken);
    Task<BudgetDto> ApproveBudgetAsync(Guid budgetId, ApproveBudgetInput input, CancellationToken cancellationToken);
    Task<BudgetDto> CreateBudgetRevisionAsync(Guid budgetId, CreateBudgetRevisionInput input, CancellationToken cancellationToken);
    Task<BudgetVarianceReportDto> GetBudgetVarianceAsync(Guid cropCycleId, CancellationToken cancellationToken);
}
