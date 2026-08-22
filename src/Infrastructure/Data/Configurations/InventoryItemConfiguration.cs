using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems", "inventory", table =>
        {
            table.HasCheckConstraint("CK_InventoryItems_Status", "\"Status\" IN ('Active', 'Archived')");
            table.HasCheckConstraint("CK_InventoryItems_ReorderLevel", "\"ReorderLevel\" IS NULL OR \"ReorderLevel\" >= 0");
            table.HasCheckConstraint("CK_InventoryItems_ExpiryRequiresLots", "\"LotTrackingPolicy\" <> 'None' OR \"ExpiryPolicy\" = 'None'");
            table.HasCheckConstraint("CK_InventoryItems_CostingMethod", "\"CostingMethod\" = 'MovingWeightedAverage'");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Code).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Category).HasConversion<string>().HasMaxLength(40);
        builder.Property(entity => entity.StockUnitCode).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.StockUnitName).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.ReorderLevel).HasPrecision(18, 6);
        builder.Property(entity => entity.LotTrackingPolicy).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.ExpiryPolicy).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.CostingMethod).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(entity => new { entity.FarmId, entity.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany()
            .HasForeignKey(entity => new { entity.StockUnitId, entity.TenantId })
            .HasPrincipalKey(unit => new { unit.Id, unit.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.FarmId, entity.Code }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.Status });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
