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
}
