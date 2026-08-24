namespace Cane360.Domain.Inventory;

public sealed class Supplier : BaseAuditableEntity
{
    private Supplier() { }

    private Supplier(Guid tenantId, Guid farmId, string code, string name, string? contact)
    {
        TenantId = tenantId;
        FarmId = farmId;
        Code = NormalizeCode(code);
        Name = name.Trim();
        Contact = string.IsNullOrWhiteSpace(contact) ? null : contact.Trim();
        Status = InventoryRecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? Contact { get; private set; }
    public InventoryRecordStatus Status { get; private set; }
    public long Version { get; private set; }

    public static Supplier Create(Guid tenantId, Guid farmId, string code, string name, string? contact)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return new Supplier(tenantId, farmId, code, name, contact);
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
        if (Version != expectedVersion) throw new InvalidOperationException("This supplier changed after it was loaded.");
    }

    private static string NormalizeCode(string code) => code.Trim().ToUpperInvariant();
}
