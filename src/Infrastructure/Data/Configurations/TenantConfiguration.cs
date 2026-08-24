using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.ToTable("Tenants", "identity");
        builder.HasKey(tenant => tenant.Id);
        builder.Property(tenant => tenant.Id).ValueGeneratedNever();
        builder.Property(tenant => tenant.TenantCode).HasMaxLength(24).IsRequired();
        builder.Property(tenant => tenant.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(tenant => tenant.TenantCode).IsUnique();
        builder.HasOne(tenant => tenant.GrowerProfile)
            .WithOne()
            .HasForeignKey<GrowerProfile>(profile => profile.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(tenant => tenant.Memberships)
            .WithOne()
            .HasForeignKey(membership => membership.TenantId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(tenant => tenant.Farms)
            .WithOne()
            .HasForeignKey(farm => farm.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(tenant => tenant.CropVarieties)
            .WithOne()
            .HasForeignKey(variety => variety.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(tenant => tenant.ActivityTypes)
            .WithOne()
            .HasForeignKey(type => type.TenantId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit<T>(EntityTypeBuilder<T> builder)
        where T : BaseAuditableEntity
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
