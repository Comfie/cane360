namespace Cane360.Application.Inventory;

public sealed record StockReceiptLineDto(
    Guid Id,
    int LineNumber,
    Guid InventoryItemId,
    Guid? InventoryLotId,
    string ItemCode,
    string ItemName,
    string? LotCode,
    DateOnly? ExpiryDate,
    string UnitCode,
    decimal Quantity,
    decimal UnitCostUsd,
    decimal LineValueUsd);
