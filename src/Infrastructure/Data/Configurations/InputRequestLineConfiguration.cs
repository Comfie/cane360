using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InputRequestLineConfiguration : IEntityTypeConfiguration<InputRequestLine>
{
    public void Configure(EntityTypeBuilder<InputRequestLine> builder)
    {
        builder.ToTable("InputRequestLines", "inventory", table =>
        {
            table.HasCheckConstraint("CK_InputRequestLines_Quantities", "\"PlannedCoverage\" > 0 AND \"PlannedRate\" > 0 AND \"PlannedQuantity\" > 0 AND \"RequestedQuantity\" > 0");
            table.HasCheckConstraint("CK_InputRequestLines_Tolerances", "\"LowerTolerancePercent\" >= 0 AND \"UpperTolerancePercent\" >= 0");
            table.HasCheckConstraint("CK_InputRequestLines_Estimate", "(\"EstimatedUnitCostUsdSnapshot\" IS NULL) = (\"EstimatedValueUsdSnapshot\" IS NULL)");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.ItemCodeSnapshot).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.ItemNameSnapshot).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.CoverageBasisSnapshot).HasConversion<string>().HasMaxLength(40);
        builder.Property(entity => entity.ApprovalRequirement).HasConversion<string>().HasMaxLength(32);
        foreach (var property in new[] { nameof(InputRequestLine.PlannedCoverage), nameof(InputRequestLine.PlannedRate), nameof(InputRequestLine.PlannedQuantity), nameof(InputRequestLine.RequestedQuantity), nameof(InputRequestLine.AvailableQuantitySnapshot) })
            builder.Property<decimal>(property).HasPrecision(18, 6);
        builder.Property(entity => entity.LowerTolerancePercent).HasPrecision(9, 6);
        builder.Property(entity => entity.UpperTolerancePercent).HasPrecision(9, 6);
        builder.Property(entity => entity.EstimatedUnitCostUsdSnapshot).HasPrecision(20, 6);
        builder.Property(entity => entity.EstimatedValueUsdSnapshot).HasPrecision(20, 6);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => new { entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(item => new { item.Id, item.TenantId, item.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryApplicationRule>().WithMany().HasForeignKey(entity => new { entity.InventoryApplicationRuleId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(rule => new { rule.Id, rule.TenantId, rule.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(entity => new { entity.UnitOfMeasureId, entity.TenantId })
            .HasPrincipalKey(unit => new { unit.Id, unit.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.InputRequestId, entity.LineNumber }).IsUnique();
        builder.HasIndex(entity => new { entity.InputRequestId, entity.InventoryItemId }).IsUnique();
    }
}
