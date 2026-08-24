using Cane360.Domain.Inventory;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Cane360.Infrastructure.Data.Configurations;

internal sealed class InputApplicationLineConfiguration : IEntityTypeConfiguration<InputApplicationLine>
{
    public void Configure(EntityTypeBuilder<InputApplicationLine> builder)
    {
        builder.ToTable("InputApplicationLines", "inventory", table => table.HasCheckConstraint("CK_InputApplicationLines_Quantity", "\"AppliedQuantity\" > 0 AND \"CoverageSnapshot\" > 0"));
        builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever(); builder.HasAlternateKey(x => new { x.Id, x.TenantId, x.FarmId });
        foreach (var property in new[] { nameof(InputApplicationLine.IssueUnitCostUsdSnapshot), nameof(InputApplicationLine.AppliedQuantity), nameof(InputApplicationLine.CoverageSnapshot), nameof(InputApplicationLine.ActualRate), nameof(InputApplicationLine.RuleRateSnapshot), nameof(InputApplicationLine.LowerTolerancePercentSnapshot), nameof(InputApplicationLine.UpperTolerancePercentSnapshot), nameof(InputApplicationLine.RateVariance) }) builder.Property(property).HasPrecision(20, 6);
        builder.Property(x => x.ItemCodeSnapshot).HasMaxLength(30); builder.Property(x => x.ItemNameSnapshot).HasMaxLength(120); builder.Property(x => x.LotCodeSnapshot).HasMaxLength(60); builder.Property(x => x.UnitCodeSnapshot).HasMaxLength(20);
        builder.HasOne<FieldReceiptLine>().WithMany().HasForeignKey(x => new { x.FieldReceiptLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<StockIssueLine>().WithMany().HasForeignKey(x => new { x.StockIssueLineId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryItem>().WithMany().HasForeignKey(x => new { x.InventoryItemId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<InventoryLot>().WithMany().HasForeignKey(x => new { x.InventoryLotId, x.TenantId, x.FarmId }).HasPrincipalKey(x => new { x.Id, x.TenantId, x.FarmId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<UnitOfMeasure>().WithMany().HasForeignKey(x => new { x.UnitOfMeasureId, x.TenantId }).HasPrincipalKey(x => new { x.Id, x.TenantId }).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => new { x.InputApplicationId, x.FieldReceiptLineId }).IsUnique(); builder.HasIndex(x => x.StockIssueLineId);
    }
}
