using Cane360.Domain.Auditing;
using Cane360.Domain.Finance;
using Cane360.Domain.Inventory;
using Cane360.Application.Finance;

namespace Cane360.Application.Common.Interfaces;

public interface IFinanceRepository
{
    Task<IFinanceTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<OperationalTransaction>> GetTransactionsAsync(Guid tenantId, Guid farmId,
        DateOnly? from, DateOnly? to, string? type, string? category, string? status, string? search,
        CancellationToken cancellationToken);
    Task<FinanceTransactionPageSource> GetTransactionPageAsync(Guid tenantId, Guid farmId,
        DateOnly? from, DateOnly? to, string? type, string? category, string? status, string? search,
        int page, int pageSize, CancellationToken cancellationToken);
    Task<OperationalTransaction?> GetTransactionAsync(Guid tenantId, Guid farmId, Guid id,
        bool trackChanges, CancellationToken cancellationToken);
    Task<OperationalTransaction?> GetTransactionByPostingKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken);
    Task<OperationalTransaction?> GetReversalByPostingKeyAsync(Guid tenantId, Guid farmId,
        string idempotencyKey, CancellationToken cancellationToken);
    Task<IReadOnlyList<OperationalCostPosting>> GetCostPostingsAsync(Guid tenantId, Guid farmId,
        Guid cropCycleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<CostSourceChain>> GetCostSourceChainsAsync(Guid tenantId, Guid farmId,
        IReadOnlyCollection<Guid> postingIds, CancellationToken cancellationToken);
    Task<IReadOnlyList<OperationalCostPosting>> GetCostPostingsForAllocationsAsync(Guid tenantId,
        Guid farmId, IReadOnlyCollection<Guid> allocationIds, CancellationToken cancellationToken);
    Task<bool> HasPayrollCostPostingAsync(Guid tenantId, Guid farmId, Guid earningLineId,
        Guid cropCycleId, CancellationToken cancellationToken);
    Task<IReadOnlyList<Budget>> GetBudgetsAsync(Guid tenantId, Guid farmId, Guid cropCycleId,
        bool trackChanges, CancellationToken cancellationToken);
    Task<Budget?> GetBudgetAsync(Guid tenantId, Guid farmId, Guid budgetId,
        bool trackChanges, CancellationToken cancellationToken);
    Task<Budget?> GetCurrentApprovedBudgetAsync(Guid tenantId, Guid farmId, Guid cropCycleId,
        bool trackChanges, CancellationToken cancellationToken);
    Task<Budget?> GetBudgetByApprovalKeyAsync(Guid tenantId, Guid farmId, string idempotencyKey,
        CancellationToken cancellationToken);
    void RemoveDraftAllocations(IReadOnlyCollection<TransactionAllocation> allocations);
    void Add(OperationalTransaction transaction);
    void Add(OperationalCostPosting posting);
    void Add(AuditEvent auditEvent);
    void Add(FinanceAuditEventLink auditLink);
    void Add(Budget budget);
    void Remove(BudgetLine line);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
