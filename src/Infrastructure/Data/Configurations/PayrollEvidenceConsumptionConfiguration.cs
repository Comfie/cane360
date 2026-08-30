using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollEvidenceConsumptionConfiguration : IEntityTypeConfiguration<PayrollEvidenceConsumption>
{
    public void Configure(EntityTypeBuilder<PayrollEvidenceConsumption> builder)
    {
        builder.ToTable("PayrollEvidenceConsumptions", "payroll"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.PayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollCalculation>().WithMany().HasForeignKey(x => new { x.PayrollCalculationId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkRecord>().WithMany().HasForeignKey(x => new { x.EvidenceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.EvidenceId).IsUnique(); builder.HasIndex(x => new { x.PayrollRunId, x.PayrollCalculationId });
    }
}
