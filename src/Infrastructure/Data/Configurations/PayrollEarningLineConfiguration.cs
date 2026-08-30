using Cane360.Domain.Labour;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollEarningLineConfiguration : IEntityTypeConfiguration<PayrollEarningLine>
{
    public void Configure(EntityTypeBuilder<PayrollEarningLine> builder)
    {
        builder.ToTable("PayrollEarningLines", "payroll", table => table.HasCheckConstraint("CK_PayrollEarningLines_Positive", "\"Quantity\" > 0 AND \"RateAmountUsd\" > 0 AND \"EarningAmountUsd\" > 0"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.EvidenceType).HasMaxLength(32).IsRequired(); builder.Property(x => x.WorkDate).HasColumnType("date"); builder.Property(x => x.ActivitySnapshot).HasColumnType("jsonb").IsRequired(); builder.Property(x => x.Quantity).HasPrecision(18, 6); builder.Property(x => x.Unit).HasMaxLength(32).IsRequired(); builder.Property(x => x.RateType).HasMaxLength(32).IsRequired(); builder.Property(x => x.RateAmountUsd).HasPrecision(18, 6); builder.Property(x => x.EarningAmountUsd).HasPrecision(18, 2); builder.Property(x => x.SourceFingerprint).HasMaxLength(64).IsRequired();
        builder.HasOne<WorkRecord>().WithMany().HasForeignKey(x => new { x.EvidenceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Attendance>().WithMany().HasForeignKey(x => new { x.AttendanceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerRate>().WithMany().HasForeignKey(x => new { x.RateSourceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerProfile>().WithMany().HasForeignKey(x => new { x.WorkerProfileId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Field>().WithMany().HasForeignKey(x => new { x.FieldId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PayrollCalculationId, x.EvidenceId }).IsUnique(); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.WorkerProfileId, x.WorkDate });
    }
}
