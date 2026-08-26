using Cane360.Domain.Auditing;
using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InventoryAuditEventLinkConfiguration : IEntityTypeConfiguration<InventoryAuditEventLink>
{
    public void Configure(EntityTypeBuilder<InventoryAuditEventLink> builder)
    {
        builder.ToTable("InventoryAuditEventLinks", "inventory", table =>
            table.HasCheckConstraint(
                "CK_InventoryAuditEventLinks_OneSubject",
                "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\", \"InventoryApplicationRuleId\", \"InputRequestId\", \"StockIssueId\", \"ManagerInvitationId\", \"FieldReceiptId\", \"InputApplicationId\", \"StockReturnId\", \"InventoryLossId\", \"OperationalCostPostingId\", \"ControlExceptionId\", \"CorrectionRecordId\", \"FieldAccountabilityCorrectionId\", \"StockCountId\", \"StockAdjustmentId\", \"InventoryLeakageExportId\") = 1"));
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.HasOne<AuditEvent>().WithMany()
            .HasForeignKey(link => new { link.AuditEventId, link.TenantId, link.FarmId })
            .HasPrincipalKey(audit => new { audit.Id, audit.TenantId, audit.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany()
            .HasForeignKey(link => new { link.UnitOfMeasureId, link.TenantId })
            .HasPrincipalKey(unit => new { unit.Id, unit.TenantId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany()
            .HasForeignKey(link => new { link.InventoryItemId, link.TenantId, link.FarmId })
            .HasPrincipalKey(item => new { item.Id, item.TenantId, item.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Supplier>().WithMany()
            .HasForeignKey(link => new { link.SupplierId, link.TenantId, link.FarmId })
            .HasPrincipalKey(supplier => new { supplier.Id, supplier.TenantId, supplier.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLot>().WithMany()
            .HasForeignKey(link => new { link.InventoryLotId, link.TenantId, link.FarmId })
            .HasPrincipalKey(lot => new { lot.Id, lot.TenantId, lot.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockReceipt>().WithMany()
            .HasForeignKey(link => new { link.StockReceiptId, link.TenantId, link.FarmId })
            .HasPrincipalKey(receipt => new { receipt.Id, receipt.TenantId, receipt.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryApplicationRule>().WithMany()
            .HasForeignKey(link => new { link.InventoryApplicationRuleId, link.TenantId, link.FarmId })
            .HasPrincipalKey(rule => new { rule.Id, rule.TenantId, rule.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InputRequest>().WithMany()
            .HasForeignKey(link => new { link.InputRequestId, link.TenantId, link.FarmId })
            .HasPrincipalKey(request => new { request.Id, request.TenantId, request.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockIssue>().WithMany()
            .HasForeignKey(link => new { link.StockIssueId, link.TenantId, link.FarmId })
            .HasPrincipalKey(issue => new { issue.Id, issue.TenantId, issue.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Cane360.Domain.Farms.ManagerInvitation>().WithMany()
            .HasForeignKey(link => new { link.ManagerInvitationId, link.TenantId, link.FarmId })
            .HasPrincipalKey(invitation => new { invitation.Id, invitation.TenantId, invitation.FarmId })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldReceipt>().WithMany().HasForeignKey(link => new { link.FieldReceiptId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InputApplication>().WithMany().HasForeignKey(link => new { link.InputApplicationId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockReturn>().WithMany().HasForeignKey(link => new { link.StockReturnId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLoss>().WithMany().HasForeignKey(link => new { link.InventoryLossId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<OperationalCostPosting>().WithMany().HasForeignKey(link => new { link.OperationalCostPostingId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ControlException>().WithMany().HasForeignKey(link => new { link.ControlExceptionId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<CorrectionRecord>().WithMany().HasForeignKey(link => new { link.CorrectionRecordId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FieldAccountabilityCorrection>().WithMany().HasForeignKey(link => new { link.FieldAccountabilityCorrectionId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockCount>().WithMany().HasForeignKey(link => new { link.StockCountId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockAdjustment>().WithMany().HasForeignKey(link => new { link.StockAdjustmentId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLeakageExport>().WithMany().HasForeignKey(link => new { link.InventoryLeakageExportId, link.TenantId, link.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(link => link.AuditEventId).IsUnique();
        builder.HasIndex(link => new { link.TenantId, link.FarmId, link.StockReceiptId });
        builder.HasIndex(link => new { link.TenantId, link.FarmId, link.InputRequestId });
        builder.HasIndex(link => new { link.TenantId, link.FarmId, link.StockIssueId });
        builder.HasIndex(link => new { link.TenantId, link.FarmId, link.StockCountId });
        builder.HasIndex(link => new { link.TenantId, link.FarmId, link.StockAdjustmentId });
        builder.HasIndex(link => new { link.TenantId, link.FarmId, link.InventoryLeakageExportId });
    }
}
