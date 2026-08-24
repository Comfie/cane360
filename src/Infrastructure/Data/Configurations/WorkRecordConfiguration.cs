using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

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
