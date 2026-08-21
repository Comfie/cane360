using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class ActivityTypeConfiguration : IEntityTypeConfiguration<ActivityType>
{
    public void Configure(EntityTypeBuilder<ActivityType> builder)
    {
        builder.ToTable("ActivityTypes", "activities", table =>
        {
            table.HasCheckConstraint("CK_ActivityTypes_PlanningMode", "\"SupportsPlanned\" OR \"SupportsUnplanned\"");
            table.HasCheckConstraint("CK_ActivityTypes_QuantityBasis", "\"QuantityBasis\" IN ('None', 'Hectares', 'StandardLines')");
            table.HasCheckConstraint("CK_ActivityTypes_Status", "\"Status\" IN ('Active', 'Archived')");
        });
        builder.HasKey(type => type.Id);
        builder.Property(type => type.Id).ValueGeneratedNever();
        builder.Property(type => type.Code).HasMaxLength(24).IsRequired();
        builder.Property(type => type.Name).HasMaxLength(100).IsRequired();
        builder.Property(type => type.QuantityBasis).HasConversion<string>().HasMaxLength(24);
        builder.Property(type => type.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(type => type.Version).IsConcurrencyToken();
        builder.HasAlternateKey(type => new { type.Id, type.TenantId });
        builder.HasIndex(type => new { type.TenantId, type.Code })
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<ActivityType> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
