using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollSettlementReopenConfiguration : IEntityTypeConfiguration<PayrollSettlementReopen>
{
    public void Configure(EntityTypeBuilder<PayrollSettlementReopen> builder)
    {
        builder.ToTable("PayrollSettlementReopens", "payroll", table => table.HasCheckConstraint("CK_PayrollSettlementReopens_Reason", "length(trim(\"Reason\")) > 0")); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.Reason).HasMaxLength(500).IsRequired(); builder.Property(x => x.ReopenedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired(); builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
        builder.HasOne<PayrollSettlementClosure>().WithOne().HasForeignKey<PayrollSettlementReopen>(x => new { x.PayrollSettlementClosureId, x.TenantId, x.FarmId }).HasPrincipalKey<PayrollSettlementClosure>(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.PayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<PayrollCalculation>().WithMany().HasForeignKey(x => new { x.PayrollCalculationId, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.ReopenedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.ReopenedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.PayrollSettlementClosureId }).IsUnique(); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique();
    }
}
