using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;
using Cane360.Infrastructure.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class CorrectionRecordConfiguration : IEntityTypeConfiguration<CorrectionRecord>
{
    public void Configure(EntityTypeBuilder<CorrectionRecord> builder)
    {
        builder.ToTable("CorrectionRecords", "inventory", table =>
            table.HasCheckConstraint("CK_CorrectionRecords_OneSource", "num_nonnulls(\"OriginalStockReceiptId\", \"OriginalStockIssueId\") = 1"));
        builder.HasKey(entity => entity.Id);
        builder.Property(entity => entity.Id).ValueGeneratedNever();
        builder.HasAlternateKey(entity => new { entity.Id, entity.TenantId, entity.FarmId });
        builder.Property(entity => entity.Reason).HasMaxLength(500).IsRequired();
        builder.Property(entity => entity.AuthorisedByUserId).HasMaxLength(450).IsRequired();
        builder.HasOne<StockReceipt>().WithMany().HasForeignKey(entity => new { entity.OriginalStockReceiptId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(receipt => new { receipt.Id, receipt.TenantId, receipt.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockIssue>().WithMany().HasForeignKey(entity => new { entity.OriginalStockIssueId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(issue => new { issue.Id, issue.TenantId, issue.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany()
            .HasForeignKey(entity => new { entity.OriginalStockMovementId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(movement => new { movement.Id, movement.TenantId, movement.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockMovement>().WithMany()
            .HasForeignKey(entity => new { entity.CorrectingStockMovementId, entity.TenantId, entity.FarmId })
            .HasPrincipalKey(movement => new { movement.Id, movement.TenantId, movement.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ApplicationUser>().WithMany().HasForeignKey(entity => entity.AuthorisedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(entity => entity.OriginalStockReceiptId);
        builder.HasIndex(entity => entity.OriginalStockIssueId);
        builder.HasIndex(entity => entity.OriginalStockMovementId).IsUnique();
        builder.HasIndex(entity => entity.CorrectingStockMovementId).IsUnique();
    }
}
