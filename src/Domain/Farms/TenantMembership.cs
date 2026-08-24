namespace Cane360.Domain.Farms;

public sealed class TenantMembership : BaseAuditableEntity
{
    private TenantMembership() { }

    private TenantMembership(Guid tenantId, string userId, string securityRole, Guid? farmId, Guid? personId)
    {
        TenantId = tenantId;
        UserId = userId;
        SecurityRole = securityRole;
        FarmId = farmId;
        PersonId = personId;
        Status = RecordStatus.Active;
    }

    public Guid TenantId { get; private set; }
    public string UserId { get; private set; } = string.Empty;
    public string SecurityRole { get; private set; } = string.Empty;
    public Guid? FarmId { get; private set; }
    public Guid? PersonId { get; private set; }
    public RecordStatus Status { get; private set; }

    internal static TenantMembership CreateGrower(Guid tenantId, string userId) =>
        new(tenantId, userId, TenantSecurityRoles.Grower, null, null);

    internal static TenantMembership CreateFarmManager(Guid tenantId, Guid farmId, string userId, Guid personId) =>
        new(tenantId, userId, TenantSecurityRoles.FarmManager, farmId, personId);
}
