using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class AdvanceApprovalConfiguration : IEntityTypeConfiguration<AdvanceApproval>
{
    public void Configure(EntityTypeBuilder<AdvanceApproval> builder)
    {
        builder.ToTable("AdvanceApprovals", "payroll", table =>
        {
            table.HasCheckConstraint("CK_AdvanceApprovals_AmountSnapshot", "\"AmountUsdSnapshot\" > 0");
            table.HasCheckConstraint("CK_AdvanceApprovals_InstallmentCountSnapshot", "\"InstallmentCountSnapshot\" > 0");
            table.HasCheckConstraint("CK_AdvanceApprovals_ScheduleSnapshot", "length(\"InstallmentScheduleSnapshot\") > 0");
        }); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.AmountUsdSnapshot).HasPrecision(18, 2); builder.Property(x => x.InstallmentScheduleSnapshot).HasMaxLength(4000).IsRequired(); builder.Property(x => x.GrowerUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.Reason).HasMaxLength(500); builder.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
        builder.HasOne<WorkerAdvance>().WithMany().HasForeignKey(x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.GrowerUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.WorkerAdvanceId, x.AdvanceVersion }).IsUnique(); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique();
    }
}
