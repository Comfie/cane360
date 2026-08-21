using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class CropVarietyConfiguration : IEntityTypeConfiguration<CropVariety>
{
    public void Configure(EntityTypeBuilder<CropVariety> builder)
    {
        builder.ToTable("CropVarieties", "farm");
        builder.HasKey(variety => variety.Id);
        builder.Property(variety => variety.Id).ValueGeneratedNever();
        builder.Property(variety => variety.Code).HasMaxLength(20).IsRequired();
        builder.Property(variety => variety.Name).HasMaxLength(80).IsRequired();
        builder.Property(variety => variety.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(variety => new { variety.TenantId, variety.Code })
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<CropVariety> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
