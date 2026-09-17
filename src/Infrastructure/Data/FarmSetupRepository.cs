using Cane360.Application.Common.Interfaces;
using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Farms;
using Cane360.Domain.Auditing;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Cane360.Infrastructure.Data;

public sealed class FarmSetupRepository(ApplicationDbContext context) : IFarmSetupRepository
{
    public async Task<Tenant?> GetTenantAdministrationContextForUserAsync(string userId,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = context.Tenants.AsSplitQuery()
            .Include(tenant => tenant.Memberships)
            .Include(tenant => tenant.ActivityTypes)
            .Include(tenant => tenant.Farms).ThenInclude(farm => farm.Persons)
                .ThenInclude(person => person.RoleAssignments)
            .Where(tenant => tenant.Memberships.Any(membership => membership.UserId == userId &&
                membership.Status == RecordStatus.Active &&
                (membership.SecurityRole == TenantSecurityRoles.Grower ||
                 membership.SecurityRole == TenantSecurityRoles.FarmManager)));
        return await (trackChanges ? query : query.AsNoTracking())
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Tenant?> GetTenantPeopleContextForUserAsync(string userId,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = context.Tenants
            .AsSingleQuery()
            .Include(tenant => tenant.Memberships)
            .Include(tenant => tenant.Farms).ThenInclude(farm => farm.Store)
            .Include(tenant => tenant.Farms).ThenInclude(farm => farm.Persons)
                .ThenInclude(person => person.RoleAssignments)
            .Where(tenant => tenant.Memberships.Any(membership => membership.UserId == userId &&
                membership.Status == RecordStatus.Active &&
                (membership.SecurityRole == TenantSecurityRoles.Grower ||
                 membership.SecurityRole == TenantSecurityRoles.FarmManager)));
        return await (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Tenant?> GetTenantReferenceContextForUserAsync(string userId,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = context.Tenants
            .AsSingleQuery()
            .Include(tenant => tenant.GrowerProfile)
            .Include(tenant => tenant.Memberships)
            .Include(tenant => tenant.Farms).ThenInclude(farm => farm.Fields)
                .ThenInclude(field => field.CropCycles).ThenInclude(cycle => cycle.HarvestResult)
            .Where(tenant => tenant.Memberships.Any(membership => membership.UserId == userId &&
                membership.Status == RecordStatus.Active &&
                (membership.SecurityRole == TenantSecurityRoles.Grower ||
                 membership.SecurityRole == TenantSecurityRoles.FarmManager)));
        return await (trackChanges ? query : query.AsNoTracking()).SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<Tenant?> GetTenantForUserAsync(
        string userId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = OperationalTenantGraph()
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

    public async Task<Tenant?> GetTenantForOperationalUserAsync(
        string userId,
        bool trackChanges,
        CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = OperationalTenantGraph()
            .Where(tenant => tenant.Memberships.Any(membership =>
                membership.UserId == userId &&
                membership.Status == RecordStatus.Active &&
                (membership.SecurityRole == TenantSecurityRoles.Grower ||
                 membership.SecurityRole == TenantSecurityRoles.FarmManager ||
                 membership.SecurityRole == TenantSecurityRoles.Supervisor)));

        if (!trackChanges)
        {
            query = query.AsNoTracking();
        }

        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<TenantSessionSummary?> GetSessionSummaryForUserAsync(
        string userId,
        CancellationToken cancellationToken) =>
        await context.TenantMemberships
            .AsNoTracking()
            .Where(membership =>
                membership.UserId == userId &&
                membership.Status == RecordStatus.Active &&
                (membership.SecurityRole == TenantSecurityRoles.Grower ||
                 membership.SecurityRole == TenantSecurityRoles.FarmManager ||
                 membership.SecurityRole == TenantSecurityRoles.Supervisor))
            .Select(membership => new TenantSessionSummary(
                membership.SecurityRole,
                context.Tenants
                    .Where(tenant => tenant.Id == membership.TenantId)
                    .Select(tenant => tenant.TenantCode)
                    .FirstOrDefault(),
                context.Farms
                    .Where(farm => farm.TenantId == membership.TenantId &&
                        farm.Status == RecordStatus.Active)
                    .Select(farm => farm.Name)
                    .FirstOrDefault()))
            .SingleOrDefaultAsync(cancellationToken);

    /// <summary>The operational tenant aggregate shared by every operational tenant resolution.</summary>
    private IQueryable<Tenant> OperationalTenantGraph() => context.Tenants
        .AsSplitQuery()
        .Include(tenant => tenant.GrowerProfile)
        .Include(tenant => tenant.Memberships)
        .Include(tenant => tenant.CropVarieties)
        .Include(tenant => tenant.ActivityTypes)
        .Include(tenant => tenant.Farms)
            .ThenInclude(farm => farm.Store)
        .Include(tenant => tenant.Farms)
            .ThenInclude(farm => farm.Persons)
                .ThenInclude(person => person.RoleAssignments)
        .Include(tenant => tenant.Farms)
            .ThenInclude(farm => farm.Fields)
                .ThenInclude(field => field.LineProfiles)
        .Include(tenant => tenant.Farms)
            .ThenInclude(farm => farm.Fields)
                .ThenInclude(field => field.CropCycles)
                    .ThenInclude(cycle => cycle.HarvestResult)
        .Include(tenant => tenant.Farms)
            .ThenInclude(farm => farm.Fields)
                .ThenInclude(field => field.CropCycles)
                    .ThenInclude(cycle => cycle.StatusChanges)
        .Include(tenant => tenant.Farms)
            .ThenInclude(farm => farm.Fields)
                .ThenInclude(field => field.CropCycles)
                    .ThenInclude(cycle => cycle.Activities)
                        .ThenInclude(activity => activity.StatusChanges)
        .Include(tenant => tenant.Farms)
            .ThenInclude(farm => farm.Fields)
                .ThenInclude(field => field.CropCycles)
                    .ThenInclude(cycle => cycle.Activities)
                        .ThenInclude(activity => activity.EvidenceLinks);

    public async Task<Tenant?> GetTenantAsync(Guid tenantId, bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<Tenant> query = BaseTenantQuery().Where(tenant => tenant.Id == tenantId);
        if (!trackChanges) query = query.AsNoTracking();
        return await query.SingleOrDefaultAsync(cancellationToken);
    }

    private IQueryable<Tenant> BaseTenantQuery() => context.Tenants
        .AsSplitQuery()
        .Include(tenant => tenant.GrowerProfile)
        .Include(tenant => tenant.Memberships)
        .Include(tenant => tenant.ActivityTypes)
        .Include(tenant => tenant.Farms).ThenInclude(farm => farm.Store)
        .Include(tenant => tenant.Farms).ThenInclude(farm => farm.Persons).ThenInclude(person => person.RoleAssignments)
        .Include(tenant => tenant.Farms).ThenInclude(farm => farm.Fields).ThenInclude(field => field.LineProfiles)
        .Include(tenant => tenant.Farms).ThenInclude(farm => farm.Fields).ThenInclude(field => field.CropCycles).ThenInclude(cycle => cycle.Activities);

    public void Add(Tenant tenant) => context.Tenants.Add(tenant);

    public async Task<IReadOnlyList<FarmSetting>> GetFarmSettingsAsync(Guid tenantId, Guid farmId,
        bool trackChanges, CancellationToken cancellationToken)
    {
        IQueryable<FarmSetting> query = context.FarmSettings.Where(item =>
            item.TenantId == tenantId && item.FarmId == farmId);
        return await (trackChanges ? query : query.AsNoTracking())
            .OrderByDescending(item => item.EffectiveFrom).ToListAsync(cancellationToken);
    }

    public void Add(FarmSetting setting) => context.FarmSettings.Add(setting);

    public void Add(AuditEvent auditEvent) => context.AuditEvents.Add(auditEvent);

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
        catch (DbUpdateException exception) when (
            exception.InnerException is PostgresException
            {
                SqlState: PostgresErrorCodes.ExclusionViolation,
                ConstraintName: "EX_FarmSettings_NoOverlap"
            })
        {
            throw new ConflictException("This setting overlaps an existing effective version.");
        }
    }
}
