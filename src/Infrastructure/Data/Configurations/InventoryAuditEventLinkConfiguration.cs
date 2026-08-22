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
                "num_nonnulls(\"UnitOfMeasureId\", \"InventoryItemId\", \"SupplierId\", \"InventoryLotId\", \"StockReceiptId\") = 1"));
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
        builder.HasIndex(link => link.AuditEventId).IsUnique();
        builder.HasIndex(link => new { link.TenantId, link.FarmId, link.StockReceiptId });
    }
}
