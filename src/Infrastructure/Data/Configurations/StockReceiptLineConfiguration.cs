using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockReceiptLineConfiguration : IEntityTypeConfiguration<StockReceiptLine>
{
    public void Configure(EntityTypeBuilder<StockReceiptLine> builder)
    {
        builder.ToTable("StockReceiptLines", "inventory", table =>
        {
            table.HasCheckConstraint("CK_StockReceiptLines_PositiveQuantity", "\"Quantity\" > 0");
            table.HasCheckConstraint("CK_StockReceiptLines_NonnegativeCost", "\"UnitCostUsd\" >= 0 AND \"LineValueUsd\" >= 0");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.ItemCodeSnapshot).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.ItemNameSnapshot).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.LotCodeSnapshot).HasMaxLength(60);
        builder.Property(entity => entity.ExpiryDateSnapshot).HasColumnType("date");
        builder.Property(entity => entity.UnitCodeSnapshot).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.Quantity).HasPrecision(18, 6);
        builder.Property(entity => entity.UnitCostUsd).HasPrecision(20, 6);
        builder.Property(entity => entity.LineValueUsd).HasPrecision(20, 6);
        builder.HasOne<InventoryItem>().WithMany()
            .HasForeignKey(entity => new { entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(item => new { item.Id, item.TenantId, item.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLot>().WithMany()
            .HasForeignKey(entity => new { entity.InventoryLotId, entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(lot => new { lot.Id, lot.InventoryItemId, lot.TenantId, lot.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany()
            .HasForeignKey(entity => new { entity.UnitOfMeasureId, entity.TenantId })
            .HasPrincipalKey(unit => new { unit.Id, unit.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.StockReceiptId, entity.LineNumber }).IsUnique();
    }
}
