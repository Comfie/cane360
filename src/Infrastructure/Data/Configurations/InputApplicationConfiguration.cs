using Cane360.Domain.Activities;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InputApplicationConfiguration : IEntityTypeConfiguration<InputApplication>
{
    public void Configure(EntityTypeBuilder<InputApplication> builder)
    {
        builder.ToTable("InputApplications", "inventory", table => table.HasCheckConstraint("CK_InputApplications_Coverage", "\"VerifiedCoverage\" > 0"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); builder.Property(x => x.CoverageBasis).HasConversion<string>().HasMaxLength(24); builder.Property(x => x.VerifiedCoverage).HasPrecision(18, 6); builder.Property(x => x.Version).IsConcurrencyToken();
        builder.Property(x => x.EnteredByUserId).HasMaxLength(450); builder.Property(x => x.SupervisorAttestationEnteredByUserId).HasMaxLength(450); builder.Property(x => x.ManagerConfirmedByUserId).HasMaxLength(450); builder.Property(x => x.ConfirmationIdempotencyKey).HasMaxLength(120); builder.Property(x => x.SupervisorAttestationNote).HasMaxLength(500); builder.Property(x => x.LateConfirmationReason).HasMaxLength(500);
        builder.HasOne<Activity>().WithMany().HasForeignKey(x => new { x.ActivityId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.SupervisorPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.EnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SupervisorAttestationEnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ManagerConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Lines).WithOne().HasForeignKey(x => new { x.InputApplicationId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.ActivityId, x.Status });
    }
}
