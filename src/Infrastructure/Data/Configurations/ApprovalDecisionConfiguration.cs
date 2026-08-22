using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class ApprovalDecisionConfiguration : IEntityTypeConfiguration<ApprovalDecision>
{
    public void Configure(EntityTypeBuilder<ApprovalDecision> builder)
    {
        builder.ToTable("ApprovalDecisions", "inventory", table =>
            table.HasCheckConstraint("CK_ApprovalDecisions_GrowerOpening", "\"ApproverRole\" = 'Grower'"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.Property(entity => entity.Outcome).HasConversion<string>().HasMaxLength(16);
        builder.Property(entity => entity.ApproverUserId).HasMaxLength(450).IsRequired();
        builder.Property(entity => entity.ApproverRole).HasMaxLength(40).IsRequired();
        builder.Property(entity => entity.Reason).HasMaxLength(500);
        builder.Property(entity => entity.IdempotencyKey).HasMaxLength(120).IsRequired();
        builder.HasOne<StockReceipt>().WithMany().HasForeignKey(entity => new { entity.StockReceiptId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(receipt => new { receipt.Id, receipt.TenantId, receipt.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.ApproverUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.IdempotencyKey).IsUnique();
        builder.HasIndex(entity => new { entity.StockReceiptId, entity.SubjectVersion }).IsUnique();
    }
}
