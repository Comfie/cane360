using Cane360.Domain.Farms;

namespace Cane360.Application.Common.Interfaces;

public interface IFarmSetupRepository
{
    Task<Tenant?> GetTenantForUserAsync(
        string userId,
        bool trackChanges,
        CancellationToken cancellationToken);

    void Add(Tenant tenant);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
