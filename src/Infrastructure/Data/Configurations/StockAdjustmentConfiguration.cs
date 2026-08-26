using Cane360.Domain.Inventory;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments", "inventory", table =>
        {
            table.HasCheckConstraint("CK_StockAdjustments_Nonzero", "\"SignedQuantity\" <> 0");
            table.HasCheckConstraint("CK_StockAdjustments_CountType", "(\"StockCountLineId\" IS NULL) OR \"AdjustmentType\" = 'CountVariance'");
            table.HasCheckConstraint("CK_StockAdjustments_ExplicitValue", "\"ExplicitUnitValueUsd\" IS NULL OR \"ExplicitUnitValueUsd\" >= 0");
        });
        builder.HasKey(entity => entity.Id); builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.AdjustmentType).HasConversion<string>().HasMaxLength(32); builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.ItemCodeSnapshot).HasMaxLength(30).IsRequired(); builder.Property(entity => entity.ItemNameSnapshot).HasMaxLength(120).IsRequired(); builder.Property(entity => entity.LotCodeSnapshot).HasMaxLength(60); builder.Property(entity => entity.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.SignedQuantity).HasPrecision(18, 6); builder.Property(entity => entity.ExplicitUnitValueUsd).HasPrecision(20, 6); builder.Property(entity => entity.UnitCostUsdSnapshot).HasPrecision(20, 6); builder.Property(entity => entity.SignedValueUsdSnapshot).HasPrecision(20, 6);
        builder.Property(entity => entity.Reason).HasMaxLength(500).IsRequired(); builder.Property(entity => entity.EventDate).HasColumnType("date"); builder.Property(entity => entity.CreatedByUserId).HasMaxLength(450).IsRequired(); builder.Property(entity => entity.CancellationReason).HasMaxLength(500);
        builder.HasOne<StockCountLine>().WithMany().HasForeignKey(entity => new { entity.StockCountLineId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(line => new { line.Id, line.TenantId, line.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>().WithMany().HasForeignKey(entity => new { entity.StoreId, entity.FarmId })
            .HasPrincipalKey(store => new { store.Id, store.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockPosition>().WithMany().HasForeignKey(entity => new { entity.StockPositionId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(position => new { position.Id, position.TenantId, position.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => new { entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(item => new { item.Id, item.TenantId, item.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLot>().WithMany().HasForeignKey(entity => new { entity.InventoryLotId, entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(lot => new { lot.Id, lot.InventoryItemId, lot.TenantId, lot.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(entity => new { entity.UnitOfMeasureId, entity.TenantId })
            .HasPrincipalKey(unit => new { unit.Id, unit.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockAdjustment>().WithMany().HasForeignKey(entity => new { entity.ReversalOfStockAdjustmentId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(adjustment => new { adjustment.Id, adjustment.TenantId, adjustment.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.StoreId, entity.Status });
        builder.HasIndex(entity => entity.StockCountLineId).HasFilter("\"StockCountLineId\" IS NOT NULL");
        builder.HasIndex(entity => entity.ReversalOfStockAdjustmentId).IsUnique().HasFilter("\"ReversalOfStockAdjustmentId\" IS NOT NULL");
    }
}
