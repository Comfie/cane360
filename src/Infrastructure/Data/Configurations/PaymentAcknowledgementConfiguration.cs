using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Payroll;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class PaymentAcknowledgementConfiguration : IEntityTypeConfiguration<PaymentAcknowledgement>
{
    public void Configure(EntityTypeBuilder<PaymentAcknowledgement> builder)
    {
        builder.ToTable("PaymentAcknowledgements", "payroll", table => table.HasCheckConstraint("CK_PaymentAcknowledgements_Status", "\"Status\" IN ('Acknowledged','Declined')"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Status).HasMaxLength(24).IsRequired(); builder.Property(x => x.CapturedByUserId).HasMaxLength(450).IsRequired(); builder.Property(x => x.EvidenceReference).HasMaxLength(200); builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired(); builder.Property(x => x.CorrelationId).HasMaxLength(128).IsRequired(); builder.Ignore(x => x.IsComplete);
        builder.HasOne<PayrollPayment>().WithOne().HasForeignKey<PaymentAcknowledgement>(x => new { x.PayrollPaymentId, x.TenantId, x.FarmId }).HasPrincipalKey<PayrollPayment>(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Farm>().WithMany().HasForeignKey(x => new { x.FarmId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.AcknowledgedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Person>().WithMany().HasForeignKey(x => new { x.CapturedByPersonId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.CapturedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.TenantId, x.FarmId, x.PayrollPaymentId }).IsUnique(); builder.HasIndex(x => new { x.TenantId, x.FarmId, x.IdempotencyKey }).IsUnique();
    }
}
