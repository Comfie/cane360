using Cane360.Domain.Activities;
using Cane360.Domain.Common;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PersonConfiguration : IEntityTypeConfiguration<Person>
{
    public void Configure(EntityTypeBuilder<Person> builder)
    {
        builder.ToTable("Persons", "farm", table =>
        {
            table.HasCheckConstraint("CK_Persons_ActiveDates", "\"ActiveTo\" IS NULL OR \"ActiveTo\" >= \"ActiveFrom\"");
            table.HasCheckConstraint("CK_Persons_Status", "\"Status\" IN ('Active', 'Archived')");
        });
        builder.HasKey(person => person.Id);
        builder.Property(person => person.Id).ValueGeneratedNever();
        builder.Property(person => person.DisplayName).HasMaxLength(120).IsRequired();
        builder.Property(person => person.Phone).HasMaxLength(30);
        builder.Property(person => person.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(person => person.Version).IsConcurrencyToken();
        builder.HasAlternateKey(person => new { person.Id, person.FarmId });
        builder.HasMany(person => person.RoleAssignments)
            .WithOne()
            .HasForeignKey(assignment => new { assignment.PersonId, assignment.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(person => new { person.FarmId, person.Status });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Person> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class PersonRoleAssignmentConfiguration : IEntityTypeConfiguration<PersonRoleAssignment>
{
    public void Configure(EntityTypeBuilder<PersonRoleAssignment> builder)
    {
        builder.ToTable("PersonRoleAssignments", "farm", table =>
        {
            table.HasCheckConstraint("CK_PersonRoleAssignments_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            table.HasCheckConstraint("CK_PersonRoleAssignments_Role", "\"Role\" IN ('FarmManager', 'Supervisor', 'Storekeeper')");
            table.HasCheckConstraint("CK_PersonRoleAssignments_PrimaryRole", "NOT \"IsPrimary\" OR \"Role\" = 'FarmManager'");
        });
        builder.HasKey(assignment => assignment.Id);
        builder.Property(assignment => assignment.Id).ValueGeneratedNever();
        builder.Property(assignment => assignment.Role).HasConversion<string>().HasMaxLength(32);
        builder.HasIndex(assignment => new { assignment.PersonId, assignment.Role })
            .IsUnique()
            .HasFilter("\"EffectiveTo\" IS NULL");
        builder.HasIndex(assignment => assignment.FarmId)
            .IsUnique()
            .HasFilter("\"Role\" = 'FarmManager' AND \"IsPrimary\" AND \"EffectiveTo\" IS NULL");
    }
}

internal sealed class FieldLineProfileConfiguration : IEntityTypeConfiguration<FieldLineProfile>
{
    public void Configure(EntityTypeBuilder<FieldLineProfile> builder)
    {
        builder.ToTable("FieldLineProfiles", "farm", table =>
        {
            table.HasCheckConstraint("CK_FieldLineProfiles_PositiveValues", "\"StandardLineLengthMetres\" > 0 AND \"EstimatedLineCount\" > 0");
            table.HasCheckConstraint("CK_FieldLineProfiles_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
        });
        builder.HasKey(profile => profile.Id);
        builder.Property(profile => profile.Id).ValueGeneratedNever();
        builder.Property(profile => profile.StandardLineLengthMetres).HasPrecision(10, 2);
        builder.Property(profile => profile.NumberingScheme).HasMaxLength(240).IsRequired();
        builder.Property(profile => profile.Version).IsConcurrencyToken();
        builder.HasAlternateKey(profile => new { profile.Id, profile.FieldId });
        builder.HasIndex(profile => profile.FieldId).IsUnique().HasFilter("\"EffectiveTo\" IS NULL");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<FieldLineProfile> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class ActivityTypeConfiguration : IEntityTypeConfiguration<ActivityType>
{
    public void Configure(EntityTypeBuilder<ActivityType> builder)
    {
        builder.ToTable("ActivityTypes", "activities", table =>
        {
            table.HasCheckConstraint("CK_ActivityTypes_PlanningMode", "\"SupportsPlanned\" OR \"SupportsUnplanned\"");
            table.HasCheckConstraint("CK_ActivityTypes_QuantityBasis", "\"QuantityBasis\" IN ('None', 'Hectares', 'StandardLines')");
            table.HasCheckConstraint("CK_ActivityTypes_Status", "\"Status\" IN ('Active', 'Archived')");
        });
        builder.HasKey(type => type.Id);
        builder.Property(type => type.Id).ValueGeneratedNever();
        builder.Property(type => type.Code).HasMaxLength(24).IsRequired();
        builder.Property(type => type.Name).HasMaxLength(100).IsRequired();
        builder.Property(type => type.QuantityBasis).HasConversion<string>().HasMaxLength(24);
        builder.Property(type => type.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(type => type.Version).IsConcurrencyToken();
        builder.HasAlternateKey(type => new { type.Id, type.TenantId });
        builder.HasIndex(type => new { type.TenantId, type.Code })
            .IsUnique()
            .HasFilter("\"Status\" = 'Active'");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<ActivityType> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

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

internal sealed class EvidenceLinkConfiguration : IEntityTypeConfiguration<EvidenceLink>
{
    public void Configure(EntityTypeBuilder<EvidenceLink> builder)
    {
        builder.ToTable("EvidenceLinks", "activities", table =>
            table.HasCheckConstraint("CK_EvidenceLinks_Role", "\"Role\" = 'SourceSheet'"));
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.Property(link => link.Role).HasConversion<string>().HasMaxLength(24);
        builder.Property(link => link.SourceSheetReference).HasMaxLength(160).IsRequired();
        builder.Property(link => link.RecordedBy).HasMaxLength(450).IsRequired();
        builder.HasOne<ApplicationUser>().WithMany()
            .HasForeignKey(link => link.RecordedBy).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(link => new { link.ActivityId, link.RecordedAt });
    }
}
