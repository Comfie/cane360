using Cane360.Domain.Payroll;
using Cane360.Domain.Labour;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class AdvanceRecoveryConfiguration : IEntityTypeConfiguration<AdvanceRecovery>
{
    public void Configure(EntityTypeBuilder<AdvanceRecovery> builder)
    {
        builder.ToTable("AdvanceRecoveries", "payroll", table => table.HasCheckConstraint("CK_AdvanceRecoveries_Amount", "\"AmountUsd\" > 0")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.AmountUsd).HasPrecision(18, 2);
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.PayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<PayrollCalculation>().WithMany().HasForeignKey(x => new { x.PayrollCalculationId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollAdvanceDeduction>().WithMany().HasForeignKey(x => new { x.PayrollAdvanceDeductionId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerAdvance>().WithMany().HasForeignKey(x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdvanceInstallment>().WithMany().HasForeignKey(x => new { x.AdvanceInstallmentId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerProfile>().WithMany().HasForeignKey(x => new { x.WorkerProfileId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.PayrollAdvanceDeductionId).IsUnique(); builder.HasIndex(x => new { x.PayrollRunId, x.PayrollCalculationId, x.WorkerAdvanceId, x.AdvanceInstallmentId }).IsUnique();
    }
}
