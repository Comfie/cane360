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

internal sealed class CropVarietyConfiguration : IEntityTypeConfiguration<CropVariety>
{
    public void Configure(EntityTypeBuilder<CropVariety> builder)
    {
        builder.ToTable("CropVarieties", "farm");
        builder.HasKey(variety => variety.Id);
        builder.Property(variety => variety.Id).ValueGeneratedNever();
        builder.Property(variety => variety.Code).HasMaxLength(20).IsRequired();
        builder.Property(variety => variety.Name).HasMaxLength(80).IsRequired();
        builder.Property(variety => variety.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(variety => new { variety.TenantId, variety.Code })
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<CropVariety> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class GrowerProfileConfiguration : IEntityTypeConfiguration<GrowerProfile>
{
    public void Configure(EntityTypeBuilder<GrowerProfile> builder)
    {
        builder.ToTable("GrowerProfiles", "identity");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(profile => profile.Phone).HasMaxLength(30);
        builder.HasIndex(profile => profile.TenantId).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<GrowerProfile> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships", "identity");
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Id).ValueGeneratedNever();
        builder.Property(membership => membership.UserId).HasMaxLength(450).IsRequired();
        builder.Property(membership => membership.SecurityRole).HasMaxLength(40).IsRequired();
        builder.Property(membership => membership.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> builder)
    {
        builder.ToTable("Farms", "farm");
        builder.HasKey(farm => farm.Id);
        builder.Property(farm => farm.Id).ValueGeneratedNever();
        builder.Property(farm => farm.Code).HasMaxLength(20).IsRequired();
        builder.Property(farm => farm.Name).HasMaxLength(120).IsRequired();
        builder.Property(farm => farm.Address).HasMaxLength(240).IsRequired();
        builder.Property(farm => farm.Location).HasMaxLength(120).IsRequired();
        builder.Property(farm => farm.Tenure).HasMaxLength(80).IsRequired();
        builder.Property(farm => farm.DeclaredHectares).HasPrecision(12, 4);
        builder.Property(farm => farm.IrrigationContext).HasMaxLength(160).IsRequired();
        builder.Property(farm => farm.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(farm => farm.TenantId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        builder.HasIndex(farm => new { farm.TenantId, farm.Code }).IsUnique();
        builder.HasAlternateKey(farm => new { farm.Id, farm.TenantId });
        builder.HasOne(farm => farm.Store)
            .WithOne()
            .HasForeignKey<Store>(store => store.FarmId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(farm => farm.Fields)
            .WithOne()
            .HasForeignKey(field => field.FarmId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(farm => farm.Persons)
            .WithOne()
            .HasForeignKey(person => person.FarmId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Farm> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class StoreConfiguration : IEntityTypeConfiguration<Store>
{
    public void Configure(EntityTypeBuilder<Store> builder)
    {
        builder.ToTable("Stores", "farm");
        builder.HasKey(store => store.Id);
        builder.Property(store => store.Id).ValueGeneratedNever();
        builder.Property(store => store.Code).HasMaxLength(20).IsRequired();
        builder.Property(store => store.Name).HasMaxLength(120).IsRequired();
        builder.Property(store => store.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(store => store.FarmId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Store> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class FieldConfiguration : IEntityTypeConfiguration<Field>
{
    public void Configure(EntityTypeBuilder<Field> builder)
    {
        builder.ToTable("Fields", "farm");
        builder.HasKey(field => field.Id);
        builder.Property(field => field.Id).ValueGeneratedNever();
        builder.Property(field => field.Code).HasMaxLength(20).IsRequired();
        builder.Property(field => field.Name).HasMaxLength(120).IsRequired();
        builder.Property(field => field.DeclaredHectares).HasPrecision(12, 4);
        builder.Property(field => field.MappedHectares).HasPrecision(12, 4);
        builder.Property(field => field.ReportingAreaSource).HasConversion<string>().HasMaxLength(24);
        builder.Property(field => field.IrrigationMethod).HasMaxLength(100).IsRequired();
        builder.Property(field => field.SoilNotes).HasMaxLength(500);
        builder.Property(field => field.Status).HasConversion<string>().HasMaxLength(24);
        builder.Ignore(field => field.ReportingHectares);
        builder.Ignore(field => field.CurrentCropCycle);
        builder.Ignore(field => field.CurrentLineProfile);
        builder.HasAlternateKey(field => new { field.Id, field.FarmId });
        builder.HasIndex(field => new { field.FarmId, field.Code })
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        builder.HasMany(field => field.CropCycles)
            .WithOne()
            .HasForeignKey(cycle => cycle.FieldId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(field => field.LineProfiles)
            .WithOne()
            .HasForeignKey(profile => profile.FieldId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Field> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class CropCycleConfiguration : IEntityTypeConfiguration<CropCycle>
{
    public void Configure(EntityTypeBuilder<CropCycle> builder)
    {
        builder.ToTable("CropCycles", "farm", table =>
        {
            table.HasCheckConstraint(
                "CK_CropCycles_CycleTypeRatoonNumber",
                "(\"CycleType\" = 'Ratoon' AND \"RatoonNumber\" > 0) OR (\"CycleType\" = 'PlantCane' AND \"RatoonNumber\" IS NULL)");
            table.HasCheckConstraint(
                "CK_CropCycles_ExpectedYieldTonnes",
                "\"ExpectedYieldTonnes\" > 0");
            table.HasCheckConstraint(
                "CK_CropCycles_HarvestWindow",
                "\"ExpectedHarvestStart\" >= \"StartDate\" AND \"ExpectedHarvestEnd\" >= \"ExpectedHarvestStart\"");
        });
        builder.HasKey(cycle => cycle.Id);
        builder.Property(cycle => cycle.Id).ValueGeneratedNever();
        builder.Property(cycle => cycle.CycleType).HasConversion<string>().HasMaxLength(24);
        builder.Property(cycle => cycle.Variety).HasMaxLength(80).IsRequired();
        builder.Property(cycle => cycle.StartDate).HasColumnType("date");
        builder.Property(cycle => cycle.ExpectedHarvestStart).HasColumnType("date");
        builder.Property(cycle => cycle.ExpectedHarvestEnd).HasColumnType("date");
        builder.Property(cycle => cycle.ExpectedYieldTonnes).HasPrecision(14, 3);
        builder.Property(cycle => cycle.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(cycle => cycle.Version).IsConcurrencyToken();
        builder.HasAlternateKey(cycle => new { cycle.Id, cycle.FieldId });
        builder.HasOne<CropVariety>()
            .WithMany()
            .HasForeignKey(cycle => cycle.CropVarietyId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(cycle => cycle.HarvestResult)
            .WithOne()
            .HasForeignKey<HarvestResult>(result => result.CropCycleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(cycle => cycle.StatusChanges)
            .WithOne()
            .HasForeignKey(change => change.CropCycleId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(cycle => cycle.Activities)
            .WithOne()
            .HasForeignKey(activity => new { activity.CropCycleId, activity.FieldId })
            .HasPrincipalKey(cycle => new { cycle.Id, cycle.FieldId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(cycle => cycle.FieldId)
            .IsUnique()
            .HasFilter("\"Status\" IN ('Active', 'ReadyForHarvest')");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<CropCycle> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class HarvestResultConfiguration : IEntityTypeConfiguration<HarvestResult>
{
    public void Configure(EntityTypeBuilder<HarvestResult> builder)
    {
        builder.ToTable("HarvestResults", "farm", table =>
            table.HasCheckConstraint("CK_HarvestResults_ActualTonnes", "\"ActualTonnes\" > 0"));
        builder.HasKey(result => result.Id);
        builder.Property(result => result.Id).ValueGeneratedNever();
        builder.Property(result => result.HarvestDate).HasColumnType("date");
        builder.Property(result => result.ActualTonnes).HasPrecision(14, 3);
        builder.HasIndex(result => result.CropCycleId).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<HarvestResult> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class CropCycleStatusChangeConfiguration : IEntityTypeConfiguration<CropCycleStatusChange>
{
    public void Configure(EntityTypeBuilder<CropCycleStatusChange> builder)
    {
        builder.ToTable("CropCycleStatusChanges", "farm");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(change => change.ToStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(change => change.RecordedAt);
        builder.Property(change => change.RecordedBy).HasMaxLength(450).IsRequired();
        builder.Property(change => change.Reason).HasMaxLength(500);
        builder.HasIndex(change => new { change.CropCycleId, change.RecordedAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(change => change.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
