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
    Task<Tenant?> GetTenantForUserAsync(
        string userId,
        bool trackChanges,
        CancellationToken cancellationToken);

    Task<Tenant?> GetTenantAsync(Guid tenantId, bool trackChanges, CancellationToken cancellationToken);

    Task<IReadOnlyList<FarmSetting>> GetFarmSettingsAsync(Guid tenantId, Guid farmId,
        bool trackChanges, CancellationToken cancellationToken);

    void Add(FarmSetting setting);
    void Add(AuditEvent auditEvent);

    void Add(Tenant tenant);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
