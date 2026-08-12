using Cane360.Application.Common.Interfaces;
using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Farms;
using Microsoft.EntityFrameworkCore;
using Npgsql;

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
            .Include(tenant => tenant.CropVarieties)
            .Include(tenant => tenant.Farms)
                .ThenInclude(farm => farm.Store)
            .Include(tenant => tenant.Farms)
                .ThenInclude(farm => farm.Fields)
                    .ThenInclude(field => field.CropCycles)
                        .ThenInclude(cycle => cycle.HarvestResult)
            .Include(tenant => tenant.Farms)
                .ThenInclude(farm => farm.Fields)
                    .ThenInclude(field => field.CropCycles)
                        .ThenInclude(cycle => cycle.StatusChanges)
            .Where(tenant => tenant.Memberships.Any(membership =>
                membership.UserId == userId &&
                membership.Status == RecordStatus.Active &&
                (membership.SecurityRole == TenantSecurityRoles.Grower ||
                 membership.SecurityRole == TenantSecurityRoles.FarmManager)));

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public void Add(Tenant tenant) => context.Tenants.Add(tenant);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            return await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException(
                "This record changed before the action could be completed. Refresh the page and try again.");
        }
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.UniqueViolation,
                ConstraintName: "IX_CropCycles_FieldId"
            })
        {
            throw new ConflictException(
                "This field already has an Active or Ready-for-harvest crop cycle.");
        }
    }
}
