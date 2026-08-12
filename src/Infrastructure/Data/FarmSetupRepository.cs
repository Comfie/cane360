using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Farms;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Infrastructure.Data;

public sealed class FarmSetupRepository(ApplicationDbContext context) : IFarmSetupRepository
{
    public async Task<Tenant?> GetTenantForUserAsync(
        string userId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = context.Tenants
            .AsSplitQuery()
            .Include(tenant => tenant.GrowerProfile)
            .Include(tenant => tenant.Memberships)
            .Include(tenant => tenant.Farms)
                .ThenInclude(farm => farm.Store)
            .Include(tenant => tenant.Farms)
                .ThenInclude(farm => farm.Fields)
                    .ThenInclude(field => field.CropCycles)
            .Where(tenant => tenant.Memberships.Any(membership =>
                membership.UserId == userId && membership.Status == RecordStatus.Active));

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(Tenant tenant) => context.Tenants.Add(tenant);

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        context.SaveChangesAsync(cancellationToken);
}
