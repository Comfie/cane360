using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollPaymentReversalConfiguration : IEntityTypeConfiguration<PayrollPaymentReversal>
{
    public void Configure(EntityTypeBuilder<PayrollPaymentReversal> builder)
    {
        builder.ToTable("PayrollPaymentReversals", "payroll", table => table.HasCheckConstraint("CK_PayrollPaymentReversals_Amount", "\"AmountUsd\" > 0 AND length(trim(\"Reason\")) > 0"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.AmountUsd).HasPrecision(18, 2); builder.Property(x => x.Reason).HasMaxLength(500).IsRequired(); builder.Property(x => x.ReversedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired(); builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
        builder.HasOne<PayrollPayment>().WithMany().HasForeignKey(x => new { x.PayrollPaymentId, x.PayrollRunId, x.PayrollCalculationId, x.CalculationVersion, x.PayrollWorkerLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.PayrollRunId, x.PayrollCalculationId, x.CalculationVersion, x.PayrollWorkerLineId, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.PayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollCalculation>().WithMany().HasForeignKey(x => new { x.PayrollCalculationId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollWorkerLine>().WithMany().HasForeignKey(x => new { x.PayrollWorkerLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.ReversedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReversedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique(); builder.HasIndex(x => x.PayrollPaymentId);
    }
}
