using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollAuditEventLinkConfiguration : IEntityTypeConfiguration<PayrollAuditEventLink>
{
    public void Configure(EntityTypeBuilder<PayrollAuditEventLink> builder)
    {
        builder.ToTable("PayrollAuditEventLinks", "payroll", table => table.HasCheckConstraint("CK_PayrollAuditEventLinks_OneSubject", "num_nonnulls(\"PayrollPeriodId\", \"WorkerAdvanceId\", \"AdvanceApprovalId\", \"AdvanceIssueId\") = 1"));
        builder.HasKey(link => link.Id); builder.Property(link => link.Id).ValueGeneratedNever(); builder.HasIndex(link => link.AuditEventId).IsUnique();
        builder.HasOne<AuditEvent>().WithMany().HasForeignKey(link => new { link.AuditEventId, link.TenantId, link.FarmId }).HasPrincipalKey(audit => new { audit.Id, audit.TenantId, audit.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Farm>().WithMany().HasForeignKey(link => new { link.FarmId, link.TenantId }).HasPrincipalKey(farm => new { farm.Id, farm.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollPeriod>().WithMany().HasForeignKey(link => new { link.PayrollPeriodId, link.TenantId, link.FarmId }).HasPrincipalKey(period => new { period.Id, period.TenantId, period.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerAdvance>().WithMany().HasForeignKey(link => new { link.WorkerAdvanceId, link.TenantId, link.FarmId }).HasPrincipalKey(advance => new { advance.Id, advance.TenantId, advance.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdvanceApproval>().WithMany().HasForeignKey(link => new { link.AdvanceApprovalId, link.TenantId, link.FarmId }).HasPrincipalKey(approval => new { approval.Id, approval.TenantId, approval.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AdvanceIssue>().WithMany().HasForeignKey(link => new { link.AdvanceIssueId, link.TenantId, link.FarmId }).HasPrincipalKey(issue => new { issue.Id, issue.TenantId, issue.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(link => new { link.TenantId, link.FarmId, link.PayrollPeriodId }); builder.HasIndex(link => new { link.TenantId, link.FarmId, link.WorkerAdvanceId });
    }
}
