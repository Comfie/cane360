using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class HarvestResultConfiguration : IEntityTypeConfiguration<HarvestResult>
{
    public void Configure(EntityTypeBuilder<HarvestResult> builder)
    {
        builder.ToTable("HarvestResults", "farm", table =>
            table.HasCheckConstraint("CK_HarvestResults_ActualTonnes", "\"ActualTonnes\" > 0"));
        builder.HasKey(result => result.Id);
        builder.Property(result => result.Id).ValueGeneratedNever();
        builder.Property(result => result.HarvestDate).HasColumnType("date");
        builder.Property(result => result.ActualTonnes).HasPrecision(14, 3);
        builder.HasIndex(result => result.CropCycleId).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<HarvestResult> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
