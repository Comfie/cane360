using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> builder)
    {
        builder.ToTable("Suppliers", "inventory", table =>
            table.HasCheckConstraint("CK_Suppliers_Status", "\"Status\" IN ('Active', 'Archived')"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Code).HasMaxLength(30).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(120).IsRequired();
        builder.Property(entity => entity.Contact).HasMaxLength(240);
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(entity => new { entity.FarmId, entity.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => new { entity.FarmId, entity.Code }).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Supplier> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
