namespace Cane360.Domain.Inventory;

public sealed class FieldReceiptLine : BaseEntity
{
    private FieldReceiptLine() { }
    private FieldReceiptLine(Guid tenantId, Guid farmId, Guid receiptId, StockIssueLine issueLine, decimal quantity)
    {
        if (quantity <= 0) throw new InvalidOperationException("Field-received quantity must be positive.");
        TenantId = tenantId; FarmId = farmId; FieldReceiptId = receiptId; StockIssueLineId = issueLine.Id;
        InventoryItemId = issueLine.InventoryItemId; InventoryLotId = issueLine.InventoryLotId;
        UnitOfMeasureId = issueLine.UnitOfMeasureId; ItemCodeSnapshot = issueLine.ItemCodeSnapshot;
        ItemNameSnapshot = issueLine.ItemNameSnapshot; LotCodeSnapshot = issueLine.LotCodeSnapshot;
        UnitCodeSnapshot = issueLine.UnitCodeSnapshot; IssueUnitCostUsdSnapshot = issueLine.IssueUnitCostUsd!.Value;
        Quantity = decimal.Round(quantity, 6, MidpointRounding.AwayFromZero);
    }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid FieldReceiptId { get; private set; }
    public Guid StockIssueLineId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? InventoryLotId { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty;
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string? LotCodeSnapshot { get; private set; }
    public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public decimal IssueUnitCostUsdSnapshot { get; private set; }
    public decimal Quantity { get; private set; }
    internal static FieldReceiptLine Create(Guid tenantId, Guid farmId, Guid receiptId, StockIssueLine issueLine, decimal quantity) =>
        new(tenantId, farmId, receiptId, issueLine, quantity);
}
