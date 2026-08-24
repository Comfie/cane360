using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Common.Interfaces;

public interface IInventoryRepository
{
    Task<IReadOnlyList<UnitOfMeasure>> GetUnitsAsync(Guid tenantId, bool trackChanges, CancellationToken cancellationToken);
    Task<UnitOfMeasure?> GetUnitAsync(Guid tenantId, Guid unitId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryItem>> GetItemsAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);
    Task<InventoryItem?> GetItemAsync(Guid tenantId, Guid farmId, Guid itemId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<Supplier>> GetSuppliersAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);
    Task<Supplier?> GetSupplierAsync(Guid tenantId, Guid farmId, Guid supplierId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryLot>> GetLotsAsync(Guid tenantId, Guid farmId, Guid? itemId, bool trackChanges, CancellationToken cancellationToken);
    Task<InventoryLot?> GetLotAsync(Guid tenantId, Guid farmId, Guid lotId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockReceipt>> GetReceiptsAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);
    Task<StockReceipt?> GetReceiptAsync(Guid tenantId, Guid farmId, Guid receiptId, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockMovement>> GetMovementsAsync(Guid tenantId, Guid farmId, Guid? itemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<(StockPosition Position, StockLedgerSnapshot Snapshot)>> GetStockOnHandAsync(Guid tenantId, Guid farmId, CancellationToken cancellationToken);
    Task<ApprovalDecision?> GetOpeningApprovalAsync(Guid receiptId, long subjectVersion, CancellationToken cancellationToken);
    Task<StockPosition?> GetPositionAsync(Guid tenantId, Guid farmId, Guid storeId, Guid itemId, Guid? lotId, bool trackChanges, CancellationToken cancellationToken);
    Task<StockLedgerSnapshot> GetPositionSnapshotAsync(Guid positionId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockMovement>> GetReceiptMovementsAsync(Guid receiptId, CancellationToken cancellationToken);
    Task<bool> HasLaterPositionMovementsAsync(IReadOnlyCollection<StockMovement> originals, CancellationToken cancellationToken);
    Task<IReadOnlyList<InventoryApplicationRule>> GetRulesAsync(Guid tenantId, Guid farmId, CancellationToken cancellationToken);
    Task<InventoryApplicationRule?> GetEffectiveRuleAsync(Guid tenantId, Guid farmId, Guid itemId, Guid activityTypeId, DateOnly date, CancellationToken cancellationToken);
    Task<(decimal Quantity, decimal ValueUsd)> GetItemStockSnapshotAsync(Guid tenantId, Guid farmId, Guid itemId, CancellationToken cancellationToken);
    Task<IReadOnlyList<InputRequest>> GetInputRequestsAsync(Guid tenantId, Guid farmId, Guid? activityId, bool trackChanges, CancellationToken cancellationToken);
    Task<InputRequest?> GetInputRequestAsync(Guid tenantId, Guid farmId, Guid requestId, bool trackChanges, CancellationToken cancellationToken);
    Task<ApprovalDecision?> GetInputRequestApprovalAsync(Guid requestId, long subjectVersion, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockIssue>> GetStockIssuesAsync(Guid tenantId, Guid farmId, Guid? requestId, bool trackChanges, CancellationToken cancellationToken);
    Task<StockIssue?> GetStockIssueAsync(Guid tenantId, Guid farmId, Guid issueId, bool trackChanges, CancellationToken cancellationToken);
    Task<decimal> GetPostedIssueQuantityAsync(Guid requestLineId, CancellationToken cancellationToken);
    Task<IReadOnlyList<StockMovement>> GetIssueMovementsAsync(Guid issueId, CancellationToken cancellationToken);
    Task<bool> HasDependentFieldAccountabilityAsync(Guid issueId, CancellationToken cancellationToken);
    Task<ManagerInvitation?> GetManagerInvitationByHashAsync(string tokenHash, bool trackChanges, CancellationToken cancellationToken);
    Task<IReadOnlyList<ManagerInvitation>> GetManagerInvitationsAsync(Guid tenantId, Guid farmId, bool trackChanges, CancellationToken cancellationToken);

    Task<IInventoryTransaction> BeginSerializableTransactionAsync(CancellationToken cancellationToken);
    void ResetTrackedChanges();
    Task LockStoreAsync(Guid tenantId, Guid farmId, Guid storeId, CancellationToken cancellationToken);
    Task LockReceiptSourceAsync(Guid tenantId, Guid farmId, Guid receiptId, CancellationToken cancellationToken);
    Task LockStockPositionsAsync(IReadOnlyCollection<Guid> positionIds, CancellationToken cancellationToken);
    Task LockInputRequestLinesAsync(IReadOnlyCollection<Guid> requestLineIds, CancellationToken cancellationToken);
    Task LockStockIssueAsync(Guid tenantId, Guid farmId, Guid issueId, CancellationToken cancellationToken);

    void Add(UnitOfMeasure unit);
    void Add(InventoryItem item);
    void Add(Supplier supplier);
    void Add(InventoryLot lot);
    void Add(StockReceipt receipt);
    void Add(StockPosition position);
    void Add(StockMovement movement);
    void Add(ApprovalDecision approval);
    void Add(CorrectionRecord correction);
    void Add(InventoryAuditEventLink auditLink);
    void Add(AuditEvent auditEvent);
    void Add(InventoryApplicationRule rule);
    void Add(InputRequest request);
    void Add(StockIssue issue);
    void Add(ManagerInvitation invitation);
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
