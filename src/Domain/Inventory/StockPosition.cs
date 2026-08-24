namespace Cane360.Domain.Inventory;

public sealed class StockPosition : BaseEntity
{
    private StockPosition() { }

    private StockPosition(Guid tenantId, Guid farmId, Guid storeId, Guid itemId, Guid? lotId)
    {
        TenantId = tenantId;
        FarmId = farmId;
        StoreId = storeId;
        InventoryItemId = itemId;
        InventoryLotId = lotId;
        PositionKey = lotId?.ToString("N") ?? "UNBATCHED";
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? InventoryLotId { get; private set; }
    public string PositionKey { get; private set; } = string.Empty;

    public static StockPosition Create(
        Guid tenantId, Guid farmId, Guid storeId, Guid itemId, Guid? lotId) =>
        new(tenantId, farmId, storeId, itemId, lotId);
}
