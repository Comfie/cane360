using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class FieldReceiptLineConfiguration : IEntityTypeConfiguration<FieldReceiptLine>
{
    public void Configure(EntityTypeBuilder<FieldReceiptLine> builder)
    {
        builder.ToTable("FieldReceiptLines", "inventory", table => table.HasCheckConstraint("CK_FieldReceiptLines_Quantity", "\"Quantity\" > 0"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        builder.Property(x => x.Quantity).HasPrecision(18, 6); builder.Property(x => x.IssueUnitCostUsdSnapshot).HasPrecision(20, 6);
        builder.Property(x => x.ItemCodeSnapshot).HasMaxLength(30); builder.Property(x => x.ItemNameSnapshot).HasMaxLength(120); builder.Property(x => x.LotCodeSnapshot).HasMaxLength(60); builder.Property(x => x.UnitCodeSnapshot).HasMaxLength(20);
        builder.HasOne<StockIssueLine>().WithMany().HasForeignKey(x => new { x.StockIssueLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => new { x.InventoryItemId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLot>().WithMany().HasForeignKey(x => new { x.InventoryLotId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(x => new { x.UnitOfMeasureId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.FieldReceiptId, x.StockIssueLineId }).IsUnique(); builder.HasIndex(x => x.StockIssueLineId);
    }
}
