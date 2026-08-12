namespace Cane360.Domain.Farms;

public sealed class TenantMembership : BaseAuditableEntity
{
    private TenantMembership() { }

    private TenantMembership(Guid tenantId, string userId)
    {
        TenantId = tenantId;
        UserId = userId;
        SecurityRole = "Grower";
        Status = RecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string SecurityRole { get; private set; } = string.Empty;
    public RecordStatus Status { get; private set; }

    internal static TenantMembership CreateGrower(Guid tenantId, string userId) => new(tenantId, userId);
}
