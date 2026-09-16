using Cane360.Domain.Farms;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FarmSettingConfiguration : IEntityTypeConfiguration<FarmSetting>
{
    public void Configure(EntityTypeBuilder<FarmSetting> builder)
    {
        builder.ToTable("FarmSettings", "farm", table =>
        {
            table.HasCheckConstraint("CK_FarmSettings_Key",
                "\"Key\" = 'ActivityLateEntryReasonDays'");
            table.HasCheckConstraint("CK_FarmSettings_Value", "\"Value\" BETWEEN 0 AND 30");
            table.HasCheckConstraint("CK_FarmSettings_Dates",
                "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.Key).HasMaxLength(80).IsRequired();
        builder.Property(item => item.Version).IsConcurrencyToken();
        builder.HasOne<Farm>().WithMany().HasForeignKey(item => new { item.FarmId, item.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(item => new { item.TenantId, item.FarmId, item.Key, item.EffectiveFrom });
        builder.Property(item => item.CreatedBy).HasMaxLength(450);
        builder.Property(item => item.LastModifiedBy).HasMaxLength(450);
    }
}
