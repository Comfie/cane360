namespace Cane360.Application.Inventory;

public sealed record CreateStockReceiptLineCommand(
    Guid InventoryItemId,
    Guid? InventoryLotId,
    decimal Quantity,
    decimal UnitCostUsd);
