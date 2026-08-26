namespace Cane360.Application.Inventory;

public sealed record CreateStockAdjustmentCommand(Guid? StockCountLineId, Guid? InventoryItemId, Guid? InventoryLotId,
    string AdjustmentType, decimal? SignedQuantity, decimal? ExplicitUnitValueUsd, string Reason, DateOnly EventDate) : IRequest<StockAdjustmentDto>;
