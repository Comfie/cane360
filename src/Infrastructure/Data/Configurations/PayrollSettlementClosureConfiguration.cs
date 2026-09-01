using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollSettlementClosureConfiguration : IEntityTypeConfiguration<PayrollSettlementClosure>
{
    public void Configure(EntityTypeBuilder<PayrollSettlementClosure> builder)
    {
        builder.ToTable("PayrollSettlementClosures", "payroll", table => table.HasCheckConstraint("CK_PayrollSettlementClosures_Totals", "\"CalculationVersion\" > 0 AND \"CloseSequence\" > 0 AND \"GrossAmountUsd\" >= 0 AND \"DeductionAmountUsd\" >= 0 AND \"NetAmountUsd\" = \"GrossAmountUsd\" - \"DeductionAmountUsd\" AND \"ActivePaymentAmountUsd\" = \"NetAmountUsd\" AND \"WorkerCount\" >= 0"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        foreach (var property in new[] { builder.Property(x => x.GrossAmountUsd), builder.Property(x => x.DeductionAmountUsd), builder.Property(x => x.NetAmountUsd), builder.Property(x => x.ActivePaymentAmountUsd) }) property.HasPrecision(18, 2);
        builder.Property(x => x.ClosedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired(); builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.PayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<PayrollCalculation>().WithMany().HasForeignKey(x => new { x.PayrollCalculationId, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.ClosedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ClosedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PayrollCalculationId, x.CalculationVersion, x.CloseSequence }).IsUnique(); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique();
    }
}
