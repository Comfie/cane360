using Cane360.Domain.Farms;
using Cane360.Domain.Common;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class CropCycleStatusChangeConfiguration : IEntityTypeConfiguration<CropCycleStatusChange>
{
    public void Configure(EntityTypeBuilder<CropCycleStatusChange> builder)
    {
        builder.ToTable("CropCycleStatusChanges", "farm");
        builder.HasKey(change => change.Id);
        builder.Property(change => change.Id).ValueGeneratedNever();
        builder.Property(change => change.FromStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(change => change.ToStatus).HasConversion<string>().HasMaxLength(32);
        builder.Property(change => change.RecordedAt);
        builder.Property(change => change.RecordedBy).HasMaxLength(450).IsRequired();
        builder.Property(change => change.Reason).HasMaxLength(500);
        builder.HasIndex(change => new { change.CropCycleId, change.RecordedAt });
        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(change => change.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
