namespace Cane360.Domain.Inventory;

public sealed class StockReceiptLine : BaseEntity
{
    private StockReceiptLine() { }

    private StockReceiptLine(
        Guid receiptId,
        Guid tenantId,
        Guid farmId,
        int lineNumber,
        InventoryItem item,
        InventoryLot? lot,
        decimal quantity,
        decimal unitCostUsd)
    {
        StockReceiptId = receiptId;
        TenantId = tenantId;
        FarmId = farmId;
        LineNumber = lineNumber;
        InventoryItemId = item.Id;
        InventoryLotId = lot?.Id;
        ItemCodeSnapshot = item.Code;
        ItemNameSnapshot = item.Name;
        LotCodeSnapshot = lot?.Code;
        ExpiryDateSnapshot = lot?.ExpiryDate;
        UnitOfMeasureId = item.StockUnitId;
        UnitCodeSnapshot = item.StockUnitCode;
        Quantity = decimal.Round(quantity, 6, MidpointRounding.AwayFromZero);
        UnitCostUsd = decimal.Round(unitCostUsd, 6, MidpointRounding.AwayFromZero);
        LineValueUsd = decimal.Round(Quantity * UnitCostUsd, 6, MidpointRounding.AwayFromZero);
    }

    public Guid StockReceiptId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? InventoryLotId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty;
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string? LotCodeSnapshot { get; private set; }
    public DateOnly? ExpiryDateSnapshot { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitCostUsd { get; private set; }
    public decimal LineValueUsd { get; private set; }

    internal static StockReceiptLine Create(
        Guid receiptId,
        Guid tenantId,
        Guid farmId,
        int lineNumber,
        InventoryItem item,
        InventoryLot? lot,
        decimal quantity,
        decimal unitCostUsd) =>
        new(receiptId, tenantId, farmId, lineNumber, item, lot, quantity, unitCostUsd);
}
