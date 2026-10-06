using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InventoryCategoryConfiguration : IEntityTypeConfiguration<InventoryCategory>
{
    public void Configure(EntityTypeBuilder<InventoryCategory> builder)
    {
        builder.ToTable("InventoryCategories", "inventory", table =>
            table.HasCheckConstraint("CK_InventoryCategories_DisplayOrder", "\"DisplayOrder\" BETWEEN 0 AND 10000"));
        builder.HasKey(category => category.Id);
        builder.Property(category => category.Id).ValueGeneratedNever();
        builder.Property(category => category.Code).HasMaxLength(40).IsRequired();
        builder.Property(category => category.NormalizedCode).HasMaxLength(40)
            .HasComputedColumnSql("upper(\"Code\")", stored: true);
        builder.HasIndex(category => new { category.TenantId, category.NormalizedCode }).IsUnique();
        builder.Property(category => category.Name).HasMaxLength(120).IsRequired();
        builder.Property(category => category.NormalizedName).HasMaxLength(120).IsRequired();
        builder.Property(category => category.Description).HasMaxLength(500);
        builder.Property(category => category.Version).IsConcurrencyToken();
        builder.HasAlternateKey(category => new { category.Code, category.TenantId });
        builder.HasIndex(category => new { category.TenantId, category.NormalizedName }).IsUnique();
        builder.Property(category => category.CreatedBy).HasMaxLength(450);
        builder.Property(category => category.LastModifiedBy).HasMaxLength(450);
    }
}
