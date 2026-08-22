namespace Cane360.Domain.Inventory;

public sealed class InventoryLot : BaseAuditableEntity
{
    private InventoryLot() { }

    private InventoryLot(Guid tenantId, Guid farmId, Guid itemId, string code, DateOnly? expiryDate)
    {
        TenantId = tenantId;
        FarmId = farmId;
        InventoryItemId = itemId;
        Code = code.Trim().ToUpperInvariant();
        ExpiryDate = expiryDate;
        Status = InventoryRecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public DateOnly? ExpiryDate { get; private set; }
    public InventoryRecordStatus Status { get; private set; }
    public long Version { get; private set; }

    public static InventoryLot Create(
        Guid tenantId, Guid farmId, InventoryItem item, string code, DateOnly? expiryDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        if (item.TenantId != tenantId || item.FarmId != farmId)
        {
            throw new InvalidOperationException("The item does not belong to this farm.");
        }
        if (item.LotTrackingPolicy == LotTrackingPolicy.None)
        {
            throw new InvalidOperationException("This item does not use lots.");
        }
        if (item.ExpiryPolicy == ExpiryPolicy.Required && expiryDate is null)
        {
            throw new InvalidOperationException("This item requires a lot expiry date.");
        }
        if (item.ExpiryPolicy == ExpiryPolicy.None && expiryDate is not null)
        {
            throw new InvalidOperationException("This item does not use expiry dates.");
        }

        return new InventoryLot(tenantId, farmId, item.Id, code, expiryDate);
    }
}
