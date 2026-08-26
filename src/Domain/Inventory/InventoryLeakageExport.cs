namespace Cane360.Domain.Inventory;

public sealed class InventoryLeakageExport : BaseEntity
{
    private InventoryLeakageExport() { }
    private InventoryLeakageExport(Guid tenantId, Guid farmId, string filterSnapshot, string exportedByUserId, DateTimeOffset exportedAt)
    { TenantId = tenantId; FarmId = farmId; FilterSnapshot = filterSnapshot; ExportedByUserId = exportedByUserId.Trim(); ExportedAt = exportedAt; }
    public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public string FilterSnapshot { get; private set; } = string.Empty; public string ExportedByUserId { get; private set; } = string.Empty; public DateTimeOffset ExportedAt { get; private set; }
    public static InventoryLeakageExport Create(Guid tenantId, Guid farmId, string filterSnapshot, string exportedByUserId, DateTimeOffset exportedAt)
    { ArgumentException.ThrowIfNullOrWhiteSpace(filterSnapshot); ArgumentException.ThrowIfNullOrWhiteSpace(exportedByUserId); return new(tenantId, farmId, filterSnapshot, exportedByUserId, exportedAt); }
}
