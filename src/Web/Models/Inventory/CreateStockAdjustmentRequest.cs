namespace Cane360.Web.Models.Inventory;

public sealed record CreateStockAdjustmentRequest(Guid? StockCountLineId, Guid? InventoryItemId, Guid? InventoryLotId,
    string AdjustmentType, decimal? SignedQuantity, decimal? ExplicitUnitValueUsd, string Reason, string EventDate);
