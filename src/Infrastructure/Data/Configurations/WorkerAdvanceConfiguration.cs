using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class WorkerAdvanceConfiguration : IEntityTypeConfiguration<WorkerAdvance>
{
    public void Configure(EntityTypeBuilder<WorkerAdvance> builder)
    {
        builder.ToTable("WorkerAdvances", "payroll", table =>
        {
            table.HasCheckConstraint("CK_WorkerAdvances_Amounts", "\"RequestedAmountUsd\" > 0 AND (\"ApprovedAmountUsd\" IS NULL OR \"ApprovedAmountUsd\" > 0)");
            table.HasCheckConstraint("CK_WorkerAdvances_Installments", "\"InstallmentCount\" > 0");
            table.HasCheckConstraint("CK_WorkerAdvances_Status", "\"Status\" IN ('Draft', 'PendingGrowerApproval', 'Approved', 'Rejected', 'Issued', 'Cancelled')");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.RequestedAmountUsd).HasPrecision(14, 2); builder.Property(x => x.ApprovedAmountUsd).HasPrecision(14, 2); builder.Property(x => x.Reason).HasMaxLength(500).IsRequired(); builder.Property(x => x.RequestedEventDate).HasColumnType("date"); builder.Property(x => x.RequestedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); builder.Property(x => x.Version).IsConcurrencyToken();
        builder.HasOne<WorkerProfile>().WithMany().HasForeignKey(x => new { x.WorkerProfileId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollPeriod>().WithMany().HasForeignKey(x => new { x.RecoveryStartPayrollPeriodId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.RequestingPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Installments).WithOne().HasForeignKey(x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.WorkerProfileId, x.Status });
    }
}
