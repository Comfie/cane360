using Cane360.Application.Finance;

namespace Cane360.Application.Common.Interfaces;

public interface IFinanceService
{
    Task<IReadOnlyList<OperationalTransactionDto>> GetTransactionsAsync(FinanceTransactionFilter filter, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> GetTransactionAsync(Guid transactionId, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> CreateAsync(CreateOperationalTransactionInput input, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> UpdateAsync(Guid transactionId, UpdateOperationalTransactionInput input, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> SetAllocationsAsync(Guid transactionId, SetTransactionAllocationsInput input, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> PostAsync(Guid transactionId, PostOperationalTransactionInput input, CancellationToken cancellationToken);
    Task<OperationalTransactionDto> ReverseAsync(Guid transactionId, ReverseOperationalTransactionInput input, CancellationToken cancellationToken);
    Task<CropCycleCostSummaryDto> GetCropCycleCostAsync(Guid cropCycleId, CancellationToken cancellationToken);
    Task<PayrollCostReconciliationDto> ReconcilePayrollAsync(CancellationToken cancellationToken);
}
