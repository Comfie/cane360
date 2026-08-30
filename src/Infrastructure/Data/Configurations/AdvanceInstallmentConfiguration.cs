using Cane360.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class AdvanceInstallmentConfiguration : IEntityTypeConfiguration<AdvanceInstallment>
{
    public void Configure(EntityTypeBuilder<AdvanceInstallment> builder)
    {
        builder.ToTable("AdvanceInstallments", "payroll", table => { table.HasCheckConstraint("CK_AdvanceInstallments_Sequence", "\"Sequence\" > 0"); table.HasCheckConstraint("CK_AdvanceInstallments_Amount", "\"AmountUsd\" > 0"); });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.AmountUsd).HasPrecision(14, 2);
        builder.HasOne<PayrollPeriod>().WithMany().HasForeignKey(x => new { x.PayrollPeriodId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WorkerAdvanceId, x.Sequence }).IsUnique();
    }
}
