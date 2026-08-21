using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class WorkRecordActivityConfiguration : IEntityTypeConfiguration<WorkRecordActivity>
{
    public void Configure(EntityTypeBuilder<WorkRecordActivity> builder)
    {
        builder.ToTable("WorkRecordActivities", "labour");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.HasOne<Activity>().WithMany()
            .HasForeignKey(link => new { link.ActivityId, link.TenantId, link.FarmId })
            .HasPrincipalKey(activity => new { activity.Id, activity.TenantId, activity.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(link => new { link.WorkRecordId, link.ActivityId }).IsUnique();
        builder.HasIndex(link => new { link.ActivityId, link.WorkRecordId });
    }
}
