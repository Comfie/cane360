using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FarmConfiguration : IEntityTypeConfiguration<Farm>
{
    public void Configure(EntityTypeBuilder<Farm> builder)
    {
        builder.ToTable("Farms", "farm");
        builder.HasKey(farm => farm.Id);
        builder.Property(farm => farm.Id).ValueGeneratedNever();
        builder.Property(farm => farm.Code).HasMaxLength(20).IsRequired();
        builder.Property(farm => farm.Name).HasMaxLength(120).IsRequired();
        builder.Property(farm => farm.Address).HasMaxLength(240).IsRequired();
        builder.Property(farm => farm.Location).HasMaxLength(120).IsRequired();
        builder.Property(farm => farm.Tenure).HasMaxLength(80).IsRequired();
        builder.Property(farm => farm.DeclaredHectares).HasPrecision(12, 4);
        builder.Property(farm => farm.IrrigationContext).HasMaxLength(160).IsRequired();
        builder.Property(farm => farm.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(farm => farm.TenantId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        builder.HasIndex(farm => new { farm.TenantId, farm.Code }).IsUnique();
        builder.HasAlternateKey(farm => new { farm.Id, farm.TenantId });
        builder.HasOne(farm => farm.Store)
            .WithOne()
            .HasForeignKey<Store>(store => store.FarmId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(farm => farm.Fields)
            .WithOne()
            .HasForeignKey(field => field.FarmId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(farm => farm.Persons)
            .WithOne()
            .HasForeignKey(person => person.FarmId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Farm> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
