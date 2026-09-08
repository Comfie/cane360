using Cane360.Domain.Auditing;
using Cane360.Domain.Finance;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FinanceAuditEventLinkConfiguration : IEntityTypeConfiguration<FinanceAuditEventLink>
{
    public void Configure(EntityTypeBuilder<FinanceAuditEventLink> builder)
    {
        builder.ToTable("FinanceAuditEventLinks", "finance", table =>
            table.HasCheckConstraint("CK_FinanceAuditEventLinks_OneSubject", "num_nonnulls(\"OperationalTransactionId\", \"TransactionAllocationId\", \"OperationalCostPostingId\") = 1"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.HasOne<AuditEvent>().WithMany().HasForeignKey(x => new { x.AuditEventId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationalTransaction>().WithMany().HasForeignKey(x => new { x.OperationalTransactionId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<TransactionAllocation>().WithMany().HasForeignKey(x => new { x.TransactionAllocationId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationalCostPosting>().WithMany().HasForeignKey(x => new { x.OperationalCostPostingId, x.TenantId, x.FarmId })
            .HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.AuditEventId, x.TenantId, x.FarmId }).IsUnique();
    }
}
