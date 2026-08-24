using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

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
