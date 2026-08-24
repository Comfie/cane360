using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class UnitOfMeasureConfiguration : IEntityTypeConfiguration<UnitOfMeasure>
{
    public void Configure(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.ToTable("UnitOfMeasures", "inventory", table =>
        {
            table.HasCheckConstraint("CK_UnitOfMeasures_DecimalPlaces", "\"DecimalPlaces\" BETWEEN 0 AND 6");
            table.HasCheckConstraint("CK_UnitOfMeasures_Status", "\"Status\" IN ('Active', 'Archived')");
        });
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Code).HasMaxLength(20).IsRequired();
        builder.Property(entity => entity.Name).HasMaxLength(80).IsRequired();
        builder.Property(entity => entity.Dimension).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId });
        builder.HasIndex(entity => new { entity.TenantId, entity.Code }).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<UnitOfMeasure> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
