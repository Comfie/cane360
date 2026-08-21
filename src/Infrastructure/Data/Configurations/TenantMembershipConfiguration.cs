using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class TenantMembershipConfiguration : IEntityTypeConfiguration<TenantMembership>
{
    public void Configure(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.ToTable("TenantMemberships", "identity");
        builder.HasKey(membership => membership.Id);
        builder.Property(membership => membership.Id).ValueGeneratedNever();
        builder.Property(membership => membership.UserId).HasMaxLength(450).IsRequired();
        builder.Property(membership => membership.SecurityRole).HasMaxLength(40).IsRequired();
        builder.Property(membership => membership.Status).HasConversion<string>().HasMaxLength(24);
        builder.HasIndex(membership => new { membership.TenantId, membership.UserId }).IsUnique();
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(membership => membership.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
