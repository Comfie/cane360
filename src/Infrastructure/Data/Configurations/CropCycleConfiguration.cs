using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

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
