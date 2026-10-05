using Cane360.Domain.Auditing;

namespace Cane360.Application.FarmSetup;

internal static class FarmProfileAudit
{
    public static void Add(IFarmSetupRepository repository, Tenant tenant, IUser user,
        TimeProvider clock, string action, string summary, string subjectType = nameof(GrowerProfile), Guid? subjectId = null)
    {
        TenantMembership membership = tenant.Memberships.Single(item => item.UserId == user.Id &&
            item.Status == RecordStatus.Active);
        repository.Add(AuditEvent.Create(tenant.Id, tenant.ActiveFarm!.Id, subjectType,
            subjectId ?? tenant.GrowerProfile.Id, action, user.Id!, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), user.CorrelationId ?? Guid.NewGuid().ToString("N"), null, summary));
    }
}
