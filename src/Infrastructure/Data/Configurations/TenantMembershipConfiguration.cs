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
        builder.ToTable("TenantMemberships", "identity", table =>
            table.HasCheckConstraint("CK_TenantMemberships_RolePerson",
                "(\"SecurityRole\" = 'Grower' AND \"FarmId\" IS NULL AND \"PersonId\" IS NULL) OR (\"SecurityRole\" = 'FarmManager' AND \"FarmId\" IS NOT NULL AND \"PersonId\" IS NOT NULL)"));
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
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(membership => new { membership.FarmId, membership.TenantId })
            .HasPrincipalKey(farm => new { FarmId = farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany()
            .HasForeignKey(membership => new { membership.PersonId, membership.FarmId })
            .HasPrincipalKey(person => new { PersonId = person.Id, FarmId = person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(membership => new { membership.PersonId, membership.FarmId }).IsUnique()
            .HasFilter("\"PersonId\" IS NOT NULL AND \"Status\" = 'Active'");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<TenantMembership> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
