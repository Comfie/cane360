using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class ActivityConfiguration : IEntityTypeConfiguration<Activity>
{
    public void Configure(EntityTypeBuilder<Activity> builder)
    {
        builder.ToTable("Activities", "activities", table =>
        {
            table.HasCheckConstraint("CK_Activities_PlannedDate", "\"Kind\" <> 'Planned' OR \"PlannedDate\" IS NOT NULL");
            table.HasCheckConstraint("CK_Activities_Quantity", "(\"QuantityBasis\" = 'None' AND \"ActualQuantity\" IS NULL) OR (\"QuantityBasis\" <> 'None' AND (\"ActualQuantity\" IS NULL OR \"ActualQuantity\" > 0))");
            table.HasCheckConstraint("CK_Activities_WholeLines", "\"QuantityBasis\" <> 'StandardLines' OR \"ActualQuantity\" IS NULL OR \"ActualQuantity\" = trunc(\"ActualQuantity\")");
            table.HasCheckConstraint("CK_Activities_EntryTime", "\"ActualEnteredAt\" IS NULL OR \"ActualAt\" IS NULL OR \"ActualEnteredAt\" >= \"ActualAt\"");
            table.HasCheckConstraint("CK_Activities_LateReason", "\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0");
            table.HasCheckConstraint("CK_Activities_EntryDelayDays", "\"EntryDelayDays\" >= 0");
            table.HasCheckConstraint("CK_Activities_RequiredActual", "\"Status\" NOT IN ('AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed') OR (\"ActualAt\" IS NOT NULL AND (\"QuantityBasis\" = 'None' OR \"ActualQuantity\" IS NOT NULL))");
            table.HasCheckConstraint("CK_Activities_Kind", "\"Kind\" IN ('Planned', 'Unplanned')");
            table.HasCheckConstraint("CK_Activities_QuantityBasis", "\"QuantityBasis\" IN ('None', 'Hectares', 'StandardLines')");
            table.HasCheckConstraint("CK_Activities_Status", "\"Status\" IN ('Draft', 'Planned', 'InProgress', 'AwaitingVerification', 'ManagerConfirmation', 'Completed', 'Closed', 'Cancelled')");
        });
        builder.HasKey(activity => activity.Id);
        builder.Property(activity => activity.Id).ValueGeneratedNever();
        builder.Property(activity => activity.ActivityTypeCode).HasMaxLength(24).IsRequired();
        builder.Property(activity => activity.ActivityTypeName).HasMaxLength(100).IsRequired();
        builder.Property(activity => activity.Kind).HasConversion<string>().HasMaxLength(24);
        builder.Property(activity => activity.QuantityBasis).HasConversion<string>().HasMaxLength(24);
        builder.Property(activity => activity.ActualQuantity).HasPrecision(12, 4);
        builder.Property(activity => activity.ActualEnteredByUserId).HasMaxLength(450);
        builder.Property(activity => activity.LateEntryReason).HasMaxLength(500);
        builder.Property(activity => activity.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(activity => activity.Version).IsConcurrencyToken();
        builder.HasAlternateKey(activity => new { activity.Id, activity.TenantId, activity.FarmId });
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(activity => new { activity.FarmId, activity.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Field>().WithMany()
            .HasForeignKey(activity => new { activity.FieldId, activity.FarmId })
            .HasPrincipalKey(field => new { field.Id, field.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ActivityType>().WithMany()
            .HasForeignKey(activity => new { activity.ActivityTypeId, activity.TenantId })
            .HasPrincipalKey(type => new { type.Id, type.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany()
            .HasForeignKey(activity => new { activity.SupervisorPersonId, activity.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldLineProfile>().WithMany()
            .HasForeignKey(activity => new { activity.FieldLineProfileId, activity.FieldId })
            .HasPrincipalKey(profile => new { profile.Id, profile.FieldId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(activity => activity.StatusChanges).WithOne()
            .HasForeignKey(change => new { change.ActivityId, change.TenantId, change.FarmId })
            .HasPrincipalKey(activity => new { activity.Id, activity.TenantId, activity.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(activity => activity.EvidenceLinks).WithOne()
            .HasForeignKey(link => new { link.ActivityId, link.TenantId, link.FarmId })
            .HasPrincipalKey(activity => new { activity.Id, activity.TenantId, activity.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(activity => activity.ActualEnteredByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(activity => new { activity.TenantId, activity.PlannedDate });
        builder.HasIndex(activity => new { activity.CropCycleId, activity.Status });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Activity> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}
