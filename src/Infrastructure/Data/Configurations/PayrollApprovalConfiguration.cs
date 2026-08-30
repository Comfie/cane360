using Cane360.Domain.Payroll;
using Cane360.Domain.Activities;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollApprovalConfiguration : IEntityTypeConfiguration<PayrollApproval>
{
    public void Configure(EntityTypeBuilder<PayrollApproval> builder)
    {
        builder.ToTable("PayrollApprovals", "payroll", table => table.HasCheckConstraint("CK_PayrollApprovals_Reason", "\"Approved\" OR length(trim(\"Reason\")) > 0"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.Reason).HasMaxLength(500); builder.Property(x => x.DecidedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.PayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollCalculation>().WithMany().HasForeignKey(x => new { x.PayrollCalculationId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.DecidedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.DecidedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.PayrollRunId, x.CalculationVersion }).IsUnique(); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique();
    }
}
