namespace Cane360.Domain.Inventory;

public sealed class InventoryItem : BaseAuditableEntity
{
    private InventoryItem() { }

    private InventoryItem(
        Guid tenantId,
        Guid farmId,
        string code,
        string name,
        InventoryItemCategory category,
        UnitOfMeasure stockUnit,
        decimal? reorderLevel,
        LotTrackingPolicy lotTrackingPolicy,
        ExpiryPolicy expiryPolicy)
    {
        TenantId = tenantId;
        FarmId = farmId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        Category = category;
        StockUnitId = stockUnit.Id;
        StockUnitCode = stockUnit.Code;
        StockUnitName = stockUnit.Name;
        ReorderLevel = reorderLevel;
        LotTrackingPolicy = lotTrackingPolicy;
        ExpiryPolicy = expiryPolicy;
        CostingMethod = InventoryCostingMethod.MovingWeightedAverage;
        Status = InventoryRecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public InventoryItemCategory Category { get; private set; }
    public Guid StockUnitId { get; private set; }
    public string StockUnitCode { get; private set; } = string.Empty;
    public string StockUnitName { get; private set; } = string.Empty;
    public decimal? ReorderLevel { get; private set; }
    public LotTrackingPolicy LotTrackingPolicy { get; private set; }
    public ExpiryPolicy ExpiryPolicy { get; private set; }
    public InventoryCostingMethod CostingMethod { get; private set; }
    public InventoryRecordStatus Status { get; private set; }
    public long Version { get; private set; }

    public static InventoryItem Create(
        Guid tenantId,
        Guid farmId,
        string code,
        string name,
        InventoryItemCategory category,
        UnitOfMeasure stockUnit,
        decimal? reorderLevel,
        LotTrackingPolicy lotTrackingPolicy,
        ExpiryPolicy expiryPolicy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (stockUnit.TenantId != tenantId || stockUnit.Status != InventoryRecordStatus.Active)
        {
            throw new InvalidOperationException("The stock unit must be an active unit in the same tenant.");
        }
        if (reorderLevel is < 0) throw new ArgumentOutOfRangeException(nameof(reorderLevel));
        if (lotTrackingPolicy == LotTrackingPolicy.None && expiryPolicy != ExpiryPolicy.None)
        {
            throw new InvalidOperationException("Expiry tracking requires lot tracking.");
        }

        return new InventoryItem(
            tenantId, farmId, code, name, category, stockUnit, reorderLevel, lotTrackingPolicy, expiryPolicy);
    }

    public void Archive(long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status == InventoryRecordStatus.Archived) return;
        Status = InventoryRecordStatus.Archived;
        Version++;
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This inventory item changed after it was loaded.");
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
