namespace Cane360.Domain.Farms;

public sealed class GrowerProfile : BaseAuditableEntity
{
    private GrowerProfile() { }

    private GrowerProfile(Guid tenantId, string displayName, string? phone)
    {
        TenantId = tenantId;
        DisplayName = displayName;
        Phone = string.IsNullOrWhiteSpace(phone) ? null : phone;
    }

    public Guid TenantId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public string? Phone { get; private set; }

    internal static GrowerProfile Create(Guid tenantId, string displayName, string? phone) =>
        new(tenantId, displayName, phone);
}
