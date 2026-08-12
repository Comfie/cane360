namespace Cane360.Domain.Farms;

public sealed class CropVariety : BaseAuditableEntity
{
    private CropVariety() { }

    private CropVariety(Guid tenantId, string code, string name)
    {
        TenantId = tenantId;
        Code = code.Trim().ToUpperInvariant();
        Name = name.Trim();
        Status = RecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public RecordStatus Status { get; private set; }

    internal static CropVariety Create(Guid tenantId, string code, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new CropVariety(tenantId, code, name);
    }
}
