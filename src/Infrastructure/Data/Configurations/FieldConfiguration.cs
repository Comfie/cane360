using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FieldConfiguration : IEntityTypeConfiguration<Field>
{
    public void Configure(EntityTypeBuilder<Field> builder)
    {
        builder.ToTable("Fields", "farm");
        builder.HasKey(field => field.Id);
        builder.Property(field => field.Id).ValueGeneratedNever();
        builder.Property(field => field.Code).HasMaxLength(20).IsRequired();
        builder.Property(field => field.Name).HasMaxLength(120).IsRequired();
        builder.Property(field => field.DeclaredHectares).HasPrecision(12, 4);
        builder.Property(field => field.MappedHectares).HasPrecision(12, 4);
        builder.Property(field => field.ReportingAreaSource).HasConversion<string>().HasMaxLength(24);
        builder.Property(field => field.IrrigationMethod).HasMaxLength(100).IsRequired();
        builder.Property(field => field.SoilNotes).HasMaxLength(500);
        builder.Property(field => field.Status).HasConversion<string>().HasMaxLength(24);
        builder.Ignore(field => field.ReportingHectares);
        builder.Ignore(field => field.CurrentCropCycle);
        builder.Ignore(field => field.CurrentLineProfile);
        builder.HasAlternateKey(field => new { field.Id, field.FarmId });
        builder.HasIndex(field => new { field.FarmId, field.Code })
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        builder.HasMany(field => field.CropCycles)
            .WithOne()
            .HasForeignKey(cycle => cycle.FieldId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(field => field.LineProfiles)
            .WithOne()
            .HasForeignKey(profile => profile.FieldId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Field> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
