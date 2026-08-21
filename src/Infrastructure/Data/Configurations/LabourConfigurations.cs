using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class WorkerProfileConfiguration : IEntityTypeConfiguration<WorkerProfile>
{
    public void Configure(EntityTypeBuilder<WorkerProfile> builder)
    {
        builder.ToTable("WorkerProfiles", "labour", table =>
        {
            table.HasCheckConstraint("CK_WorkerProfiles_EmploymentType", "\"EmploymentType\" IN ('Permanent', 'Seasonal', 'Casual', 'Contract', 'TaskBased')");
            table.HasCheckConstraint("CK_WorkerProfiles_ActiveDates", "\"ActiveTo\" IS NULL OR \"ActiveTo\" >= \"ActiveFrom\"");
            table.HasCheckConstraint("CK_WorkerProfiles_Status", "\"Status\" IN ('Active', 'Archived')");
            table.HasCheckConstraint("CK_WorkerProfiles_ProtectedNationalId", "octet_length(\"NationalIdCiphertext\") > 0 AND octet_length(\"NationalIdNonce\") = 12 AND octet_length(\"NationalIdTag\") = 16 AND octet_length(\"NationalIdFingerprint\") = 32");
        });
        builder.HasKey(worker => worker.Id);
        builder.Property(worker => worker.Id).ValueGeneratedNever();
        builder.Property(worker => worker.EmploymentType).HasConversion<string>().HasMaxLength(24);
        builder.Property(worker => worker.ActiveFrom).HasColumnType("date");
        builder.Property(worker => worker.ActiveTo).HasColumnType("date");
        builder.Property(worker => worker.Status).HasConversion<string>().HasMaxLength(24);
        builder.Property(worker => worker.NationalIdCiphertext).IsRequired();
        builder.Property(worker => worker.NationalIdNonce).IsRequired();
        builder.Property(worker => worker.NationalIdTag).IsRequired();
        builder.Property(worker => worker.NationalIdKeyId).HasMaxLength(64).IsRequired();
        builder.Property(worker => worker.NationalIdFingerprint).IsRequired();
        builder.Property(worker => worker.NationalIdMask).HasMaxLength(16).IsRequired();
        builder.Property(worker => worker.Version).IsConcurrencyToken();
        builder.HasAlternateKey(worker => new { worker.Id, worker.TenantId, worker.FarmId });
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(worker => new { worker.FarmId, worker.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany()
            .HasForeignKey(worker => new { worker.PersonId, worker.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(worker => new { worker.FarmId, worker.PersonId }).IsUnique();
        builder.HasIndex(worker => new { worker.FarmId, worker.NationalIdFingerprint })
            .IsUnique()
            .HasDatabaseName("UX_WorkerProfiles_Farm_NationalIdFingerprint");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkerProfile> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class WorkerRateConfiguration : IEntityTypeConfiguration<WorkerRate>
{
    public void Configure(EntityTypeBuilder<WorkerRate> builder)
    {
        builder.ToTable("WorkerRates", "labour", table =>
        {
            table.HasCheckConstraint("CK_WorkerRates_Basis", "\"Basis\" IN ('Daily', 'Monthly', 'Hectare', 'StandardLine')");
            table.HasCheckConstraint("CK_WorkerRates_PositiveRate", "\"RateUsd\" > 0");
            table.HasCheckConstraint("CK_WorkerRates_EffectiveDates", "\"EffectiveTo\" IS NULL OR \"EffectiveTo\" >= \"EffectiveFrom\"");
            table.HasCheckConstraint("CK_WorkerRates_ActivityScope", "((\"Basis\" IN ('Hectare', 'StandardLine')) = (\"ActivityTypeId\" IS NOT NULL))");
        });
        builder.HasKey(rate => rate.Id);
        builder.Property(rate => rate.Id).ValueGeneratedNever();
        builder.Property(rate => rate.Basis).HasConversion<string>().HasMaxLength(24);
        builder.Property(rate => rate.RateUsd).HasPrecision(12, 4);
        builder.Property(rate => rate.EffectiveFrom).HasColumnType("date");
        builder.Property(rate => rate.EffectiveTo).HasColumnType("date");
        builder.Property(rate => rate.Version).IsConcurrencyToken();
        builder.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(rate => new { rate.WorkerProfileId, rate.TenantId, rate.FarmId })
            .HasPrincipalKey(worker => new { worker.Id, worker.TenantId, worker.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ActivityType>().WithMany()
            .HasForeignKey(rate => new { rate.ActivityTypeId, rate.TenantId })
            .HasPrincipalKey(type => new { type.Id, type.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(rate => new { rate.WorkerProfileId, rate.Basis, rate.ActivityTypeId, rate.EffectiveFrom });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkerRate> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances", "labour", table =>
        {
            table.HasCheckConstraint("CK_Attendances_Status", "\"Status\" IN ('Present', 'Absent')");
            table.HasCheckConstraint("CK_Attendances_FieldAllocation", "(\"Status\" = 'Present' AND \"FieldId\" IS NOT NULL) OR (\"Status\" = 'Absent' AND \"FieldId\" IS NULL)");
            table.HasCheckConstraint("CK_Attendances_EntryDelay", "\"EntryDelayDays\" >= 0 AND (\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0)");
        });
        builder.HasKey(attendance => attendance.Id);
        builder.Property(attendance => attendance.Id).ValueGeneratedNever();
        builder.Property(attendance => attendance.WorkDate).HasColumnType("date");
        builder.Property(attendance => attendance.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(attendance => attendance.EnteredByUserId).HasMaxLength(450).IsRequired();
        builder.Property(attendance => attendance.LateEntryReason).HasMaxLength(500);
        builder.Property(attendance => attendance.Version).IsConcurrencyToken();
        builder.HasAlternateKey(attendance => new { attendance.Id, attendance.TenantId, attendance.FarmId });
        builder.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(attendance => new { attendance.WorkerProfileId, attendance.TenantId, attendance.FarmId })
            .HasPrincipalKey(worker => new { worker.Id, worker.TenantId, worker.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Field>().WithMany()
            .HasForeignKey(attendance => new { attendance.FieldId, attendance.FarmId })
            .HasPrincipalKey(field => new { field.Id, field.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(attendance => attendance.EnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(attendance => new { attendance.WorkerProfileId, attendance.WorkDate })
            .IsUnique()
            .HasDatabaseName("UX_Attendances_Worker_WorkDate");
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<Attendance> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

internal sealed class WorkRecordConfiguration : IEntityTypeConfiguration<WorkRecord>
{
    public void Configure(EntityTypeBuilder<WorkRecord> builder)
    {
        builder.ToTable("WorkRecords", "labour", table =>
        {
            table.HasCheckConstraint("CK_WorkRecords_Basis", "\"PayBasis\" IN ('Daily', 'Monthly', 'Hectare', 'StandardLine')");
            table.HasCheckConstraint("CK_WorkRecords_Status", "\"Status\" IN ('Draft', 'SupervisorVerified', 'Confirmed', 'Cancelled', 'Superseded')");
            table.HasCheckConstraint("CK_WorkRecords_Quantity", "((\"PayBasis\" IN ('Hectare', 'StandardLine')) AND \"Quantity\" > 0) OR ((\"PayBasis\" IN ('Daily', 'Monthly')) AND \"Quantity\" IS NULL)");
            table.HasCheckConstraint("CK_WorkRecords_WholeLines", "\"PayBasis\" <> 'StandardLine' OR \"Quantity\" = trunc(\"Quantity\")");
            table.HasCheckConstraint("CK_WorkRecords_MonthlyDeferred", "\"PayBasis\" <> 'Monthly' OR \"CalculatedAmountUsd\" IS NULL");
            table.HasCheckConstraint("CK_WorkRecords_EntryDelay", "\"EntryDelayDays\" >= 0 AND (\"EntryDelayDays\" <= 2 OR length(trim(\"LateEntryReason\")) > 0)");
        });
        builder.HasKey(record => record.Id);
        builder.Property(record => record.Id).ValueGeneratedNever();
        builder.Property(record => record.WorkDate).HasColumnType("date");
        builder.Property(record => record.PayBasis).HasConversion<string>().HasMaxLength(24);
        builder.Property(record => record.AppliedRateUsd).HasPrecision(12, 4);
        builder.Property(record => record.RateEffectiveFrom).HasColumnType("date");
        builder.Property(record => record.RateEffectiveTo).HasColumnType("date");
        builder.Property(record => record.Quantity).HasPrecision(12, 4);
        builder.Property(record => record.CalculatedAmountUsd).HasPrecision(14, 2);
        builder.Property(record => record.EnteredByUserId).HasMaxLength(450).IsRequired();
        builder.Property(record => record.LateEntryReason).HasMaxLength(500);
        builder.Property(record => record.Status).HasConversion<string>().HasMaxLength(32);
        builder.Property(record => record.SupersededByUserId).HasMaxLength(450);
        builder.Property(record => record.CorrectionReason).HasMaxLength(500);
        builder.Property(record => record.Version).IsConcurrencyToken();
        builder.HasAlternateKey(record => new { record.Id, record.TenantId, record.FarmId });
        builder.HasOne<Attendance>().WithMany()
            .HasForeignKey(record => new { record.AttendanceId, record.TenantId, record.FarmId })
            .HasPrincipalKey(attendance => new { attendance.Id, attendance.TenantId, attendance.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerProfile>().WithMany()
            .HasForeignKey(record => new { record.WorkerProfileId, record.TenantId, record.FarmId })
            .HasPrincipalKey(worker => new { worker.Id, worker.TenantId, worker.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Field>().WithMany()
            .HasForeignKey(record => new { record.FieldId, record.FarmId })
            .HasPrincipalKey(field => new { field.Id, field.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerRate>().WithMany().HasForeignKey(record => record.WorkerRateId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(record => record.EnteredByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(record => record.SupersededByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(record => record.Activities).WithOne(link => link.WorkRecord)
            .HasForeignKey(link => new { link.WorkRecordId, link.TenantId, link.FarmId })
            .HasPrincipalKey(record => new { record.Id, record.TenantId, record.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(record => record.Scopes).WithOne()
            .HasForeignKey(scope => new { scope.WorkRecordId, scope.TenantId, scope.FarmId })
            .HasPrincipalKey(record => new { record.Id, record.TenantId, record.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(record => record.Verification).WithOne()
            .HasForeignKey<WorkVerification>(verification => new { verification.WorkRecordId, verification.TenantId, verification.FarmId })
            .HasPrincipalKey<WorkRecord>(record => new { record.Id, record.TenantId, record.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(record => new { record.AttendanceId, record.PayBasis })
            .IsUnique()
            .HasFilter("\"Status\" NOT IN ('Cancelled', 'Superseded') AND \"PayBasis\" IN ('Daily', 'Monthly')")
            .HasDatabaseName("UX_WorkRecords_Attendance_TimeBasis");
        builder.HasIndex(record => new { record.FarmId, record.WorkDate });
        ConfigureAudit(builder);
    }

    private static void ConfigureAudit(EntityTypeBuilder<WorkRecord> builder)
    {
        builder.Property(entity => entity.CreatedBy).HasMaxLength(450);
        builder.Property(entity => entity.LastModifiedBy).HasMaxLength(450);
    }
}

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

internal sealed class WorkScopeConfiguration : IEntityTypeConfiguration<WorkScope>
{
    public void Configure(EntityTypeBuilder<WorkScope> builder)
    {
        builder.ToTable("WorkScopes", "labour", table =>
        {
            table.HasCheckConstraint("CK_WorkScopes_Type", "\"ScopeType\" IN ('LineRange', 'NamedSection')");
            table.HasCheckConstraint("CK_WorkScopes_Shape", "(\"ScopeType\" = 'LineRange' AND \"FieldLineProfileId\" IS NOT NULL AND \"StartLine\" > 0 AND \"EndLine\" >= \"StartLine\" AND \"SectionName\" IS NULL AND \"NormalizedSectionName\" IS NULL) OR (\"ScopeType\" = 'NamedSection' AND \"FieldLineProfileId\" IS NULL AND \"StartLine\" IS NULL AND \"EndLine\" IS NULL AND length(trim(\"NormalizedSectionName\")) > 0)");
        });
        builder.HasKey(scope => scope.Id);
        builder.Property(scope => scope.Id).ValueGeneratedNever();
        builder.Property(scope => scope.ScopeType).HasConversion<string>().HasMaxLength(24);
        builder.Property(scope => scope.SectionName).HasMaxLength(120);
        builder.Property(scope => scope.NormalizedSectionName).HasMaxLength(120);
        builder.HasOne<Activity>().WithMany()
            .HasForeignKey(scope => new { scope.ActivityId, scope.TenantId, scope.FarmId })
            .HasPrincipalKey(activity => new { activity.Id, activity.TenantId, activity.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldLineProfile>().WithMany().HasForeignKey(scope => scope.FieldLineProfileId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(scope => new { scope.ActivityId, scope.NormalizedSectionName })
            .HasFilter("\"ScopeType\" = 'NamedSection' AND \"SupersededAt\" IS NULL")
            .HasDatabaseName("IX_WorkScopes_Activity_NamedSection");
    }
}

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

internal sealed class AuditEventConfiguration : IEntityTypeConfiguration<AuditEvent>
{
    public void Configure(EntityTypeBuilder<AuditEvent> builder)
    {
        builder.ToTable("AuditEvents", "audit");
        builder.HasKey(audit => audit.Id);
        builder.Property(audit => audit.Id).ValueGeneratedNever();
        builder.Property(audit => audit.SubjectType).HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.Action).HasMaxLength(80).IsRequired();
        builder.Property(audit => audit.AuthenticatedUserId).HasMaxLength(450).IsRequired();
        builder.Property(audit => audit.SecurityRole).HasMaxLength(40).IsRequired();
        builder.Property(audit => audit.CorrelationId).HasMaxLength(120).IsRequired();
        builder.Property(audit => audit.Reason).HasMaxLength(500);
        builder.Property(audit => audit.SafeSummary).HasMaxLength(500).IsRequired();
        builder.HasOne<Farm>().WithMany()
            .HasForeignKey(audit => new { audit.FarmId, audit.TenantId })
            .HasPrincipalKey(farm => new { farm.Id, farm.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany()
            .HasForeignKey(audit => new { audit.OperationalPersonId, audit.FarmId })
            .HasPrincipalKey(person => new { person.Id, person.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(audit => audit.AuthenticatedUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(audit => new { audit.TenantId, audit.FarmId, audit.SubjectType, audit.SubjectId, audit.OccurredAt });
    }
}
