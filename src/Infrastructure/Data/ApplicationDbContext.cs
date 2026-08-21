using System.Reflection;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Farms;
using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Cane360.Infrastructure.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>, IApplicationDbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<GrowerProfile> GrowerProfiles => Set<GrowerProfile>();
    public DbSet<TenantMembership> TenantMemberships => Set<TenantMembership>();
    public DbSet<Farm> Farms => Set<Farm>();
    public DbSet<Store> Stores => Set<Store>();
    public DbSet<Field> Fields => Set<Field>();
    public DbSet<CropVariety> CropVarieties => Set<CropVariety>();
    public DbSet<CropCycle> CropCycles => Set<CropCycle>();
    public DbSet<HarvestResult> HarvestResults => Set<HarvestResult>();
    public DbSet<CropCycleStatusChange> CropCycleStatusChanges => Set<CropCycleStatusChange>();
    public DbSet<Person> Persons => Set<Person>();
    public DbSet<PersonRoleAssignment> PersonRoleAssignments => Set<PersonRoleAssignment>();
    public DbSet<FieldLineProfile> FieldLineProfiles => Set<FieldLineProfile>();
    public DbSet<ActivityType> ActivityTypes => Set<ActivityType>();
    public DbSet<Activity> Activities => Set<Activity>();
    public DbSet<ActivityStatusChange> ActivityStatusChanges => Set<ActivityStatusChange>();
    public DbSet<EvidenceLink> EvidenceLinks => Set<EvidenceLink>();
    public DbSet<WorkerProfile> WorkerProfiles => Set<WorkerProfile>();
    public DbSet<WorkerRate> WorkerRates => Set<WorkerRate>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<WorkRecord> WorkRecords => Set<WorkRecord>();
    public DbSet<WorkRecordActivity> WorkRecordActivities => Set<WorkRecordActivity>();
    public DbSet<WorkScope> WorkScopes => Set<WorkScope>();
    public DbSet<WorkVerification> WorkVerifications => Set<WorkVerification>();
    public DbSet<AuditEvent> AuditEvents => Set<AuditEvent>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasPostgresExtension("btree_gist");
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
