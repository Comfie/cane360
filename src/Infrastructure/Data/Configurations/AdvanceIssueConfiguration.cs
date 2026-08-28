using Cane360.Domain.Activities;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class AdvanceIssueConfiguration : IEntityTypeConfiguration<AdvanceIssue>
{
    public void Configure(EntityTypeBuilder<AdvanceIssue> builder)
    {
        builder.ToTable("AdvanceIssues", "payroll", table => { table.HasCheckConstraint("CK_AdvanceIssues_Amount", "\"AmountUsd\" > 0"); table.HasCheckConstraint("CK_AdvanceIssues_Method", "(\"PaymentMethod\" = 'Cash' AND \"PayingPersonId\" IS NOT NULL AND \"ReceivingWorkerId\" IS NOT NULL AND \"WorkerAcknowledged\" = true) OR (\"PaymentMethod\" = 'MobileMoney' AND \"Provider\" IS NOT NULL AND \"MaskedRecipientNumber\" IS NOT NULL AND \"ExternalReference\" IS NOT NULL AND \"TransactionStatus\" IS NOT NULL AND length(trim(\"Provider\")) > 0 AND length(trim(\"MaskedRecipientNumber\")) > 0 AND length(trim(\"ExternalReference\")) > 0 AND length(trim(\"TransactionStatus\")) > 0)"); });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20); builder.Property(x => x.AmountUsd).HasPrecision(14, 2); builder.Property(x => x.RecordedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.Provider).HasMaxLength(100); builder.Property(x => x.MaskedRecipientNumber).HasMaxLength(32); builder.Property(x => x.ExternalReference).HasMaxLength(160); builder.Property(x => x.TransactionStatus).HasMaxLength(64); builder.Property(x => x.IdempotencyKey).HasMaxLength(120).IsRequired();
        builder.HasOne<WorkerAdvance>().WithMany().HasForeignKey(x => new { x.WorkerAdvanceId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.PayingPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<WorkerProfile>().WithMany().HasForeignKey(x => new { x.ReceivingWorkerId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict); builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique(); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.ExternalReference }).IsUnique().HasFilter("\"ExternalReference\" IS NOT NULL"); builder.HasIndex(x => x.WorkerAdvanceId).IsUnique();
    }
}
