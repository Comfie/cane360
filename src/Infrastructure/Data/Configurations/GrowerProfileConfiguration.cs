using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class GrowerProfileConfiguration : IEntityTypeConfiguration<GrowerProfile>
{
    public void Configure(EntityTypeBuilder<GrowerProfile> builder)
    {
        builder.ToTable("GrowerProfiles", "identity");
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(profile => profile.Phone).HasMaxLength(30);
        builder.HasIndex(profile => profile.TenantId).IsUnique();
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<GrowerProfile> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
