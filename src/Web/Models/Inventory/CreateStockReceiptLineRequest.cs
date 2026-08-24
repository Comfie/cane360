namespace Cane360.Web.Models.Inventory;

public sealed record CreateStockReceiptLineRequest(
    Guid InventoryItemId, Guid? InventoryLotId, decimal Quantity, decimal UnitCostUsd);
