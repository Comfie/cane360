using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ToTable("PayrollPeriods", "payroll", table =>
        {
            table.HasCheckConstraint("CK_PayrollPeriods_Month", "\"Month\" BETWEEN 1 AND 12");
            table.HasCheckConstraint("CK_PayrollPeriods_Dates", "\"StartDate\" = make_date(\"Year\", \"Month\", 1) AND \"EndDate\" = (make_date(\"Year\", \"Month\", 1) + interval '1 month - 1 day')::date");
            table.HasCheckConstraint("CK_PayrollPeriods_Status", "\"Status\" IN ('Draft', 'Open', 'Closed', 'Cancelled')");
            table.HasCheckConstraint("CK_PayrollPeriods_ClosedMetadata", "(\"Status\" = 'Closed' AND \"ClosedAt\" IS NOT NULL AND \"ClosedByUserId\" IS NOT NULL AND \"ClosedByPayrollRunId\" IS NOT NULL) OR (\"Status\" <> 'Closed' AND \"ClosedAt\" IS NULL AND \"ClosedByUserId\" IS NULL AND \"ClosedByPersonId\" IS NULL AND \"ClosedByPayrollRunId\" IS NULL)");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.StartDate).HasColumnType("date"); builder.Property(x => x.EndDate).HasColumnType("date");
        builder.Property(x => x.DisplayName).HasMaxLength(32).IsRequired(); builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);
        builder.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.OpenedByUserId).HasMaxLength(450); builder.Property(x => x.CancelledByUserId).HasMaxLength(450); builder.Property(x => x.ClosedByUserId).HasMaxLength(450); builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.Property(x => x.Version).IsConcurrencyToken(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.CreatedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.OpenedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.CancelledByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.ClosedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.OpenedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.ClosedByPayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FarmId, x.Year, x.Month }).IsUnique().HasDatabaseName("UX_PayrollPeriods_Farm_Year_Month");
    }
}
