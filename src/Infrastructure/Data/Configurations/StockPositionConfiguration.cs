using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockPositionConfiguration : IEntityTypeConfiguration<StockPosition>
{
    public void Configure(EntityTypeBuilder<StockPosition> builder)
    {
        builder.ToTable("StockPositions", "inventory");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.PositionKey).HasMaxLength(32).IsRequired();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.HasOne<Farm>().WithMany().HasForeignKey(entity => new { entity.FarmId, entity.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Store>().WithMany().HasForeignKey(entity => new { entity.StoreId, entity.FarmId })
            .HasPrincipalKey(store => new { Id = store.Id, store.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(entity => new { entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(item => new { item.Id, item.TenantId, item.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLot>().WithMany()
            .HasForeignKey(entity => new { entity.InventoryLotId, entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(lot => new { lot.Id, lot.InventoryItemId, lot.TenantId, lot.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.StoreId, entity.InventoryItemId, entity.PositionKey }).IsUnique();
    }
}
