using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FieldLineProfileConfiguration : IEntityTypeConfiguration<FieldLineProfile>
{
    public void Configure(EntityTypeBuilder<FieldLineProfile> builder)
    {
        builder.ToTable("FieldLineProfiles", "farm", table =>
        {
            table.HasCheckConstraint("CK_FieldLineProfiles_PositiveValues", "\"StandardLineLengthMetres\" > 0 AND \"EstimatedLineCount\" > 0");
            table.HasCheckConstraint("CK_FieldLineProfiles_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
        });
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.StandardLineLengthMetres).HasPrecision(10, 2);
        builder.Property(profile => profile.NumberingScheme).HasMaxLength(240).IsRequired();
        builder.Property(profile => profile.Version).IsConcurrencyToken();
        builder.HasAlternateKey(profile => new { profile.Id, profile.FieldId });
        builder.HasIndex(profile => profile.FieldId).IsUnique().HasFilter("\"EffectiveTo\" IS NULL");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<FieldLineProfile> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
