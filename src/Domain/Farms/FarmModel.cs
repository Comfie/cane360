namespace Cane360.Domain.Farms;

public sealed class FarmModel : BaseAuditableEntity
{
    private FarmModel() { }
    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public bool Active { get; private set; } = true;
    public long Version { get; private set; } = 1;

    public static FarmModel Create(Guid tenantId, string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (tenantId == Guid.Empty || code.Trim().Length > 24 || name.Trim().Length > 100)
            throw new ArgumentException("Farm Model requires a tenant, code (24) and name (100).");
        return new FarmModel { TenantId = tenantId, Code = code.Trim().ToUpperInvariant(), Name = name.Trim() };
    }

    public void Update(string name, bool active, long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This Farm Model changed after it was loaded.");
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (name.Trim().Length > 100) throw new ArgumentException("Farm Model name exceeds 100 characters.");
        Name = name.Trim();
        Active = active;
        Version++;
    }
}
