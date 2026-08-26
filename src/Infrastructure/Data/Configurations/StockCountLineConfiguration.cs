using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockCountLineConfiguration : IEntityTypeConfiguration<StockCountLine>
{
    public void Configure(EntityTypeBuilder<StockCountLine> builder)
    {
        builder.ToTable("StockCountLines", "inventory", table =>
        {
            table.HasCheckConstraint("CK_StockCountLines_ExpectedNonnegative", "\"ExpectedQuantity\" >= 0 AND \"ExpectedValueUsd\" >= 0");
            table.HasCheckConstraint("CK_StockCountLines_CountedNonnegative", "\"CountedQuantity\" IS NULL OR \"CountedQuantity\" >= 0");
        });
        builder.HasKey(entity => entity.Id); builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.ItemCodeSnapshot).HasMaxLength(30).IsRequired(); builder.Property(entity => entity.ItemNameSnapshot).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.LotCodeSnapshot).HasMaxLength(60); builder.Property(entity => entity.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.ExpectedQuantity).HasPrecision(18, 6); builder.Property(entity => entity.ExpectedValueUsd).HasPrecision(20, 6);
        builder.Property(entity => entity.CountedQuantity).HasPrecision(18, 6); builder.Property(entity => entity.Notes).HasMaxLength(1000); builder.Property(entity => entity.EnteredByUserId).HasMaxLength(450);
        builder.HasOne<StockCount>().WithMany(entity => entity.Lines).HasForeignKey(entity => new { entity.StockCountId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(count => new { count.Id, count.TenantId, count.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockPosition>().WithMany().HasForeignKey(entity => new { entity.StockPositionId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(position => new { position.Id, position.TenantId, position.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => new { entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(item => new { item.Id, item.TenantId, item.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLot>().WithMany().HasForeignKey(entity => new { entity.InventoryLotId, entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(lot => new { lot.Id, lot.InventoryItemId, lot.TenantId, lot.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(entity => new { entity.UnitOfMeasureId, entity.TenantId })
            .HasPrincipalKey(unit => new { unit.Id, unit.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockAdjustment>().WithMany().HasForeignKey(entity => new { entity.PostedStockAdjustmentId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(adjustment => new { adjustment.Id, adjustment.TenantId, adjustment.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.EnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.StockCountId, entity.StockPositionId }).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.InventoryItemId, entity.InventoryLotId });
    }
}
