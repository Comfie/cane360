using Cane360.Domain.Payroll;
using Cane360.Domain.Labour;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollAdvanceDeductionConfiguration : IEntityTypeConfiguration<PayrollAdvanceDeduction>
{
    public void Configure(EntityTypeBuilder<PayrollAdvanceDeduction> builder)
    {
        builder.ToTable("PayrollAdvanceDeductions", "payroll", table => table.HasCheckConstraint("CK_PayrollAdvanceDeductions_Amounts", "\"AmountUsd\" > 0 AND \"OutstandingBeforeUsd\" >= \"AmountUsd\" AND \"ScheduledAmountUsd\" >= \"OutstandingBeforeUsd\""));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.ScheduledAmountUsd).HasPrecision(18, 2); builder.Property(x => x.OutstandingBeforeUsd).HasPrecision(18, 2); builder.Property(x => x.AmountUsd).HasPrecision(18, 2);
        builder.HasOne<WorkerAdvance>().WithMany().HasForeignKey(x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdvanceInstallment>().WithMany().HasForeignKey(x => new { x.AdvanceInstallmentId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollPeriod>().WithMany().HasForeignKey(x => new { x.RecoveryPayrollPeriodId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerProfile>().WithMany().HasForeignKey(x => new { x.WorkerProfileId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PayrollCalculationId, x.WorkerAdvanceId, x.AdvanceInstallmentId }).IsUnique();
    }
}
