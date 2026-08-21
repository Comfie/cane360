using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class WorkVerificationConfiguration : IEntityTypeConfiguration<WorkVerification>
{
    public void Configure(EntityTypeBuilder<WorkVerification> builder)
    {
        builder.ToTable("WorkVerifications", "labour", table =>
            table.HasCheckConstraint("CK_WorkVerifications_ConfirmationTime", "\"ManagerConfirmedAt\" IS NULL OR \"ManagerConfirmedAt\" >= \"SupervisorVerifiedAt\""));
        builder.HasKey(verification => verification.Id);
        builder.Property(verification => verification.Id).ValueGeneratedNever();
        builder.Property(verification => verification.SupervisorVerificationEnteredByUserId).HasMaxLength(450).IsRequired();
        builder.Property(verification => verification.ManagerConfirmedByUserId).HasMaxLength(450);
        builder.HasOne<Person>().WithMany()
            .HasForeignKey(verification => new { verification.SupervisorPersonId, verification.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(verification => verification.SupervisorVerificationEnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(verification => verification.ManagerConfirmedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(verification => verification.WorkRecordId).IsUnique();
    }
}
