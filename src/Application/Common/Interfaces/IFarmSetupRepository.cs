using Cane360.Domain.Farms;

namespace Cane360.Application.Common.Interfaces;

public interface IFarmSetupRepository
{
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

    void Add(Tenant tenant);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
