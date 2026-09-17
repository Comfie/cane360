using Cane360.Domain.Farms;
using Cane360.Domain.Auditing;

namespace Cane360.Application.Common.Interfaces;

public interface IFarmSetupRepository
{
    /// <summary>Administration membership, personnel, and reference context without operational history.</summary>
    Task<Tenant?> GetTenantAdministrationContextForUserAsync(string userId, bool trackChanges,
        CancellationToken cancellationToken);
    /// <summary>Membership and field/cycle references, without operational history or personnel graphs.</summary>
    Task<Tenant?> GetTenantReferenceContextForUserAsync(string userId, bool trackChanges,
        CancellationToken cancellationToken);
    /// <summary>Membership and farm personnel references, without fields or operational history.</summary>
    Task<Tenant?> GetTenantPeopleContextForUserAsync(string userId, bool trackChanges,
        CancellationToken cancellationToken);
    /// <summary>
    /// Operational tenant aggregate for a Grower or FarmManager membership only. Returns null for
    /// every other role, so callers that do not add their own role gate stay default-deny.
    /// </summary>
    Task<Tenant?> GetTenantForUserAsync(
        string userId,
        bool trackChanges,
        CancellationToken cancellationToken);

    /// <summary>
    /// The same operational tenant aggregate, additionally resolving for a Supervisor membership.
    /// Reserved for the approved Supervisor MVP surface (field/activity capture); do not widen other
    /// callers onto it without a deliberate authorisation decision.
    /// </summary>
    Task<Tenant?> GetTenantForOperationalUserAsync(
        string userId,
        bool trackChanges,
        CancellationToken cancellationToken);

    /// <summary>
    /// Scalar session projection for any signed-in tenant role (Grower, FarmManager, or Supervisor).
    /// Loads no aggregate; it exists so the session endpoint does not pull the whole farm graph.
    /// </summary>
    Task<TenantSessionSummary?> GetSessionSummaryForUserAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<Tenant?> GetTenantAsync(Guid tenantId, bool trackChanges, CancellationToken cancellationToken);

    Task<IReadOnlyList<FarmSetting>> GetFarmSettingsAsync(Guid tenantId, Guid farmId,
        bool trackChanges, CancellationToken cancellationToken);

    void Add(FarmSetting setting);
    void Add(AuditEvent auditEvent);

    void Add(Tenant tenant);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
