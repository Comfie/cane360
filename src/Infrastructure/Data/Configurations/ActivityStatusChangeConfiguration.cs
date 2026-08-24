using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class ActivityStatusChangeConfiguration : IEntityTypeConfiguration<ActivityStatusChange>
{
    public void Configure(EntityTypeBuilder<ActivityStatusChange> builder)
    {
        builder.ToTable("ActivityStatusChanges", "activities", table =>
        {
            table.HasCheckConstraint("CK_ActivityStatusChanges_Status", "\"FromStatus\" IN ('Draft', 'Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed') AND \"ToStatus\" IN ('Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed', 'Cancelled')");
            table.HasCheckConstraint("CK_ActivityStatusChanges_CancellationReason", "\"ToStatus\" <> 'Cancelled' OR length(trim(\"Reason\")) > 0");
        });
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(change => change.ToStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(change => change.RecordedBy).HasMaxLength(450).IsRequired();
        builder.Property(change => change.Reason).HasMaxLength(500);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(change => change.RecordedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany()
            .HasForeignKey(change => new { change.OperationalPersonId, change.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(change => new { change.ActivityId, change.RecordedAt });
    }
}
