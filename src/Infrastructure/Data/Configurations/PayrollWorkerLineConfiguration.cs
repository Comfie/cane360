using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollWorkerLineConfiguration : IEntityTypeConfiguration<PayrollWorkerLine>
{
    public void Configure(EntityTypeBuilder<PayrollWorkerLine> builder)
    {
        builder.ToTable("PayrollWorkerLines", "payroll", table => table.HasCheckConstraint("CK_PayrollWorkerLines_Totals", "\"GrossAmountUsd\" > 0 AND \"DeductionAmountUsd\" >= 0 AND \"NetAmountUsd\" >= 0 AND \"NetAmountUsd\" = \"GrossAmountUsd\" - \"DeductionAmountUsd\""));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.HasAlternateKey(x => new { x.Id, x.PayrollCalculationId, x.WorkerProfileId, x.TenantId, x.FarmId }); builder.Property(x => x.WorkerNameSnapshot).HasMaxLength(200).IsRequired();
        builder.Property(x => x.GrossAmountUsd).HasPrecision(18, 2); builder.Property(x => x.DeductionAmountUsd).HasPrecision(18, 2); builder.Property(x => x.NetAmountUsd).HasPrecision(18, 2);
        builder.HasOne<WorkerProfile>().WithMany().HasForeignKey(x => new { x.WorkerProfileId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.EarningLines).WithOne().HasForeignKey(x => new { x.PayrollWorkerLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.AdvanceDeductions).WithOne().HasForeignKey(x => new { x.PayrollWorkerLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PayrollCalculationId, x.WorkerProfileId }).IsUnique();
    }
}
