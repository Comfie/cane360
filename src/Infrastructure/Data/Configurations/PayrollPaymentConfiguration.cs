using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PayrollPaymentConfiguration : IEntityTypeConfiguration<PayrollPayment>
{
    public void Configure(EntityTypeBuilder<PayrollPayment> builder)
    {
        builder.ToTable("PayrollPayments", "payroll", table =>
        {
            table.HasCheckConstraint("CK_PayrollPayments_Amount", "\"AmountUsd\" > 0");
            table.HasCheckConstraint("CK_PayrollPayments_Version", "\"CalculationVersion\" > 0");
            table.HasCheckConstraint("CK_PayrollPayments_Method", "(\"Method\" = 'Cash' AND \"ExternalStatus\" = 'Posted' AND \"Provider\" IS NULL AND \"RecipientCiphertext\" IS NULL AND \"TransactionReference\" IS NULL) OR (\"Method\" = 'MobileMoney' AND \"Provider\" IS NOT NULL AND \"RecipientCiphertext\" IS NOT NULL AND \"RecipientNonce\" IS NOT NULL AND \"RecipientTag\" IS NOT NULL AND \"RecipientKeyId\" IS NOT NULL AND \"MaskedRecipientNumber\" IS NOT NULL AND \"TransactionReference\" IS NOT NULL AND \"ExternalStatus\" IN ('Posted','Successful','Pending','Failed'))");
            table.HasCheckConstraint("CK_PayrollPayments_ProtectedRecipient", "\"RecipientCiphertext\" IS NULL OR (octet_length(\"RecipientCiphertext\") > 0 AND octet_length(\"RecipientNonce\") = 12 AND octet_length(\"RecipientTag\") = 16)");
        });
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId }); builder.HasAlternateKey(x => new { x.Id, x.PayrollRunId, x.PayrollCalculationId, x.CalculationVersion, x.PayrollWorkerLineId, x.TenantId, x.FarmId });
        builder.Property(x => x.Method).HasConversion<string>().HasMaxLength(20);
        builder.Property(x => x.AmountUsd).HasPrecision(18, 2); builder.Property(x => x.PaymentDate).HasColumnType("date");
        builder.Property(x => x.ExternalStatus).HasMaxLength(32).IsRequired(); builder.Property(x => x.Provider).HasMaxLength(80);
        builder.Property(x => x.RecipientKeyId).HasMaxLength(80); builder.Property(x => x.MaskedRecipientNumber).HasMaxLength(32);
        builder.Property(x => x.TransactionReference).HasMaxLength(160); builder.Property(x => x.RecordedByUserId).HasMaxLength(450).IsRequired();
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired(); builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired();
        builder.Ignore(x => x.ContributesToPaidAmount);
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollRun>().WithMany().HasForeignKey(x => new { x.PayrollRunId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollCalculation>().WithMany().HasForeignKey(x => new { x.PayrollCalculationId, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.PayrollRunId, x.CalculationVersion, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PayrollWorkerLine>().WithMany().HasForeignKey(x => new { x.PayrollWorkerLineId, x.PayrollCalculationId, x.WorkerProfileId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.PayrollCalculationId, x.WorkerProfileId, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkerProfile>().WithMany().HasForeignKey(x => new { x.WorkerProfileId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.RecordedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.RecordedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique();
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.Provider, x.TransactionReference }).IsUnique().HasFilter("\"TransactionReference\" IS NOT NULL");
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.PayrollRunId, x.PayrollCalculationId, x.CalculationVersion, x.PayrollWorkerLineId });
    }
}
