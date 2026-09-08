namespace Cane360.Domain.Finance;

public sealed class FinanceAuditEventLink : BaseEntity
{
    private FinanceAuditEventLink() { }

    private FinanceAuditEventLink(Guid auditEventId, Guid tenantId, Guid farmId,
        Guid? transactionId, Guid? allocationId, Guid? costPostingId)
    {
        AuditEventId = auditEventId;
        TenantId = tenantId;
        FarmId = farmId;
        OperationalTransactionId = transactionId;
        TransactionAllocationId = allocationId;
        OperationalCostPostingId = costPostingId;
    }

    public Guid AuditEventId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid? OperationalTransactionId { get; private set; }
    public Guid? TransactionAllocationId { get; private set; }
    public Guid? OperationalCostPostingId { get; private set; }

    public static FinanceAuditEventLink ForTransaction(Guid audit, Guid tenant, Guid farm, Guid id) =>
        new(audit, tenant, farm, id, null, null);
    public static FinanceAuditEventLink ForAllocation(Guid audit, Guid tenant, Guid farm, Guid id) =>
        new(audit, tenant, farm, null, id, null);
    public static FinanceAuditEventLink ForCostPosting(Guid audit, Guid tenant, Guid farm, Guid id) =>
        new(audit, tenant, farm, null, null, id);
}
