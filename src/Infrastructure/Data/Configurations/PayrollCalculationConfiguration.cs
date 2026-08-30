using Cane360.Domain.Payroll;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollCalculationConfiguration : IEntityTypeConfiguration<PayrollCalculation>
{
    public void Configure(EntityTypeBuilder<PayrollCalculation> builder)
    {
        builder.ToTable("PayrollCalculations", "payroll", table => { table.HasCheckConstraint("CK_PayrollCalculations_Totals", "\"GrossAmountUsd\" >= 0 AND \"DeductionAmountUsd\" >= 0 AND \"NetAmountUsd\" >= 0 AND \"NetAmountUsd\" = \"GrossAmountUsd\" - \"DeductionAmountUsd\""); table.HasCheckConstraint("CK_PayrollCalculations_Version", "\"CalculationVersion\" > 0"); });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        Money(builder.Property(x => x.GrossAmountUsd)); Money(builder.Property(x => x.DeductionAmountUsd)); Money(builder.Property(x => x.NetAmountUsd));
        builder.Property(x => x.BlockerSnapshot).HasColumnType("jsonb").IsRequired(); builder.Property(x => x.SourceFingerprint).HasMaxLength(64).IsRequired(); builder.Property(x => x.CalculatedByUserId).HasMaxLength(450).IsRequired();
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.PayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollPeriod>().WithMany().HasForeignKey(x => new { x.PayrollPeriodId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.CalculatedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CalculatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.WorkerLines).WithOne().HasForeignKey(x => new { x.PayrollCalculationId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PayrollRunId, x.CalculationVersion }).IsUnique();
    }
    private static void Money(PropertyBuilder<decimal> property) => property.HasPrecision(18, 2);
}
