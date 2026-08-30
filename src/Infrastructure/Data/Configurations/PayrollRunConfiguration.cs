using Cane360.Domain.Farms;
using Cane360.Domain.Activities;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns", "payroll", table =>
        {
            table.HasCheckConstraint("CK_PayrollRuns_Status", "\"Status\" IN ('Draft','Calculated','PendingGrowerApproval','Approved','Rejected','Cancelled')");
            table.HasCheckConstraint("CK_PayrollRuns_CalculationVersions", "\"LatestCalculationVersion\" >= 0 AND (\"SubmittedCalculationVersion\" IS NULL OR (\"SubmittedCalculationVersion\" > 0 AND \"SubmittedCalculationVersion\" <= \"LatestCalculationVersion\"))");
            table.HasCheckConstraint("CK_PayrollRuns_SubmissionState", "(\"Status\" = 'PendingGrowerApproval' AND \"SubmittedCalculationVersion\" IS NOT NULL AND \"SubmittedAt\" IS NOT NULL AND \"SubmittedByUserId\" IS NOT NULL) OR \"Status\" <> 'PendingGrowerApproval'");
            table.HasCheckConstraint("CK_PayrollRuns_DecisionState", "(\"Status\" = 'Approved' AND \"ApprovedAt\" IS NOT NULL) OR (\"Status\" = 'Rejected' AND \"RejectedAt\" IS NOT NULL AND length(trim(\"RejectionReason\")) > 0) OR \"Status\" NOT IN ('Approved','Rejected')");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(32); builder.Property(x => x.Version).IsConcurrencyToken(); builder.Property(x => x.CreatedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.SubmittedByUserId).HasMaxLength(450); builder.Property(x => x.RejectionReason).HasMaxLength(500); builder.Property(x => x.CancellationReason).HasMaxLength(500);
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.CreatedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollPeriod>().WithMany().HasForeignKey(x => new { x.PayrollPeriodId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict); builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.SubmittedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FarmId, x.PayrollPeriodId }).HasFilter("\"Status\" <> 'Cancelled'").IsUnique().HasDatabaseName("UX_PayrollRuns_ActivePeriod");
    }
}
