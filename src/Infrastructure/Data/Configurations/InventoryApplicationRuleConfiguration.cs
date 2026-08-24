using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InventoryApplicationRuleConfiguration : IEntityTypeConfiguration<InventoryApplicationRule>
{
    public void Configure(EntityTypeBuilder<InventoryApplicationRule> builder)
    {
        builder.ToTable("InventoryApplicationRules", "inventory", table =>
        {
            table.HasCheckConstraint("CK_InventoryApplicationRules_Dates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            table.HasCheckConstraint("CK_InventoryApplicationRules_Rate", "\"RatePerCoverageUnit\" > 0");
            table.HasCheckConstraint("CK_InventoryApplicationRules_Tolerances", "\"LowerTolerancePercent\" >= 0 AND \"UpperTolerancePercent\" >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.CoverageBasis).HasConversion<string>().HasMaxLength(40);
        builder.Property(entity => entity.RatePerCoverageUnit).HasPrecision(18, 6);
        builder.Property(entity => entity.LowerTolerancePercent).HasPrecision(9, 6);
        builder.Property(entity => entity.UpperTolerancePercent).HasPrecision(9, 6);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasOne<Farm>().WithMany().HasForeignKey(entity => new { entity.FarmId, entity.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => new { entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(item => new { item.Id, item.TenantId, item.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ActivityType>().WithMany().HasForeignKey(entity => new { entity.ActivityTypeId, entity.TenantId })
            .HasPrincipalKey(type => new { type.Id, type.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(entity => new { entity.UnitOfMeasureId, entity.TenantId })
            .HasPrincipalKey(unit => new { unit.Id, unit.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.InventoryItemId, entity.ActivityTypeId, entity.EffectiveFrom });
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
