using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class StockCountConfiguration : IEntityTypeConfiguration<StockCount>
{
    public void Configure(EntityTypeBuilder<StockCount> builder)
    {
        builder.ToTable("StockCounts", "inventory");
        builder.HasKey(entity => entity.Id); builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(entity => entity.Notes).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.CountingPersons).HasMaxLength(1000).IsRequired();
        builder.Property(entity => entity.EventDate).HasColumnType("date");
        builder.Property(entity => entity.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(entity => entity.CancellationReason).HasMaxLength(500);
        builder.HasOne<Store>().WithMany().HasForeignKey(entity => new { entity.StoreId, entity.FarmId })
            .HasPrincipalKey(store => new { store.Id, store.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.TenantId, entity.FarmId, entity.StoreId, entity.Status });
        builder.HasIndex(entity => new { entity.StoreId, entity.Status }).IsUnique()
            .HasFilter("\"Status\" = 'InProgress'");
    }
}
