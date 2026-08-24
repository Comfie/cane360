using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class ManagerInvitationConfiguration : IEntityTypeConfiguration<ManagerInvitation>
{
    public void Configure(EntityTypeBuilder<ManagerInvitation> builder)
    {
        builder.ToTable("ManagerInvitations", "identity");
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.TokenHash).HasMaxLength(64).IsRequired();
        builder.Property(entity => entity.CreatedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(entity => entity.RevokedByUserId).HasMaxLength(450);
        builder.Property(entity => entity.RedeemedByUserId).HasMaxLength(450);
        builder.Property(entity => entity.Version).IsConcurrencyToken();
        builder.HasOne<Tenant>().WithMany().HasForeignKey(entity => entity.TenantId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Farm>().WithMany().HasForeignKey(entity => new { entity.FarmId, entity.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(entity => new { entity.PersonId, entity.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.RevokedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.RedeemedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.TokenHash).IsUnique();
        builder.HasIndex(entity => new { entity.TenantId, entity.PersonId }).IsUnique()
            .HasFilter("\"RevokedAt\" IS NULL AND \"RedeemedAt\" IS NULL");
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
