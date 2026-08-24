using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InventoryLotConfiguration : IEntityTypeConfiguration<InventoryLot>
{
    public void Configure(EntityTypeBuilder<InventoryLot> builder)
    {
        builder.ToTable("InventoryLots", "inventory", table =>
            table.HasCheckConstraint("CK_InventoryLots_Status", "\"Status\" IN ('Active', 'Archived')"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Code).HasMaxLength(60).IsRequired();
        builder.Property(entity => entity.ExpiryDate).HasColumnType("date");
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.HasAlternateKey(entity => new { entity.Id, entity.InventoryItemId, entity.TenantId, entity.FarmId });
        builder.HasOne<InventoryItem>().WithMany()
            .HasForeignKey(entity => new { entity.InventoryItemId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(item => new { item.Id, item.TenantId, item.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.InventoryItemId, entity.Code }).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<InventoryLot> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
