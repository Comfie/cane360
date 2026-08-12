using System.Reflection;
using Cane360.Application.Common.Interfaces;
using Cane360.Domain.Farms;
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
    public DbSet<CropCycle> CropCycles => Set<CropCycle>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
    }
}
