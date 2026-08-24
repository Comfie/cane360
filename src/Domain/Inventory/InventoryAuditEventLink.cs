namespace Cane360.Domain.Inventory;

public sealed class InventoryAuditEventLink : BaseEntity
{
    private InventoryAuditEventLink() { }

    private InventoryAuditEventLink(Guid auditEventId, Guid tenantId, Guid farmId)
    {
        AuditEventId = auditEventId;
        TenantId = tenantId;
        FarmId = farmId;
    }

    public Guid AuditEventId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid? UnitOfMeasureId { get; private set; }
    public Guid? InventoryItemId { get; private set; }
    public Guid? SupplierId { get; private set; }
    public Guid? InventoryLotId { get; private set; }
    public Guid? StockReceiptId { get; private set; }
    public Guid? InventoryApplicationRuleId { get; private set; }
    public Guid? InputRequestId { get; private set; }
    public Guid? StockIssueId { get; private set; }
    public Guid? ManagerInvitationId { get; private set; }
    public Guid? FieldReceiptId { get; private set; }
    public Guid? InputApplicationId { get; private set; }
    public Guid? StockReturnId { get; private set; }
    public Guid? InventoryLossId { get; private set; }
    public Guid? OperationalCostPostingId { get; private set; }
    public Guid? ControlExceptionId { get; private set; }
    public Guid? CorrectionRecordId { get; private set; }
    public Guid? FieldAccountabilityCorrectionId { get; private set; }

    public static InventoryAuditEventLink ForUnit(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid unitId) =>
        new(auditEventId, tenantId, farmId) { UnitOfMeasureId = unitId };

    public static InventoryAuditEventLink ForItem(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid itemId) =>
        new(auditEventId, tenantId, farmId) { InventoryItemId = itemId };

    public static InventoryAuditEventLink ForSupplier(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid supplierId) =>
        new(auditEventId, tenantId, farmId) { SupplierId = supplierId };

    public static InventoryAuditEventLink ForLot(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid lotId) =>
        new(auditEventId, tenantId, farmId) { InventoryLotId = lotId };

    public static InventoryAuditEventLink ForReceipt(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid receiptId) =>
        new(auditEventId, tenantId, farmId) { StockReceiptId = receiptId };

    public static InventoryAuditEventLink ForRule(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid ruleId) =>
        new(auditEventId, tenantId, farmId) { InventoryApplicationRuleId = ruleId };

    public static InventoryAuditEventLink ForRequest(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid requestId) =>
        new(auditEventId, tenantId, farmId) { InputRequestId = requestId };

    public static InventoryAuditEventLink ForIssue(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid issueId) =>
        new(auditEventId, tenantId, farmId) { StockIssueId = issueId };

    public static InventoryAuditEventLink ForInvitation(
        Guid auditEventId, Guid tenantId, Guid farmId, Guid invitationId) =>
        new(auditEventId, tenantId, farmId) { ManagerInvitationId = invitationId };

    public static InventoryAuditEventLink ForFieldReceipt(Guid auditEventId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditEventId, tenantId, farmId) { FieldReceiptId = id };
    public static InventoryAuditEventLink ForApplication(Guid auditEventId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditEventId, tenantId, farmId) { InputApplicationId = id };
    public static InventoryAuditEventLink ForReturn(Guid auditEventId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditEventId, tenantId, farmId) { StockReturnId = id };
    public static InventoryAuditEventLink ForLoss(Guid auditEventId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditEventId, tenantId, farmId) { InventoryLossId = id };
    public static InventoryAuditEventLink ForCost(Guid auditEventId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditEventId, tenantId, farmId) { OperationalCostPostingId = id };
    public static InventoryAuditEventLink ForException(Guid auditEventId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditEventId, tenantId, farmId) { ControlExceptionId = id };
    public static InventoryAuditEventLink ForCorrection(Guid auditEventId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditEventId, tenantId, farmId) { CorrectionRecordId = id };
    public static InventoryAuditEventLink ForFieldAccountabilityCorrection(Guid auditEventId, Guid tenantId, Guid farmId, Guid id) =>
        new(auditEventId, tenantId, farmId) { FieldAccountabilityCorrectionId = id };
}
