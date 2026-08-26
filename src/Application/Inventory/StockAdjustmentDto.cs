namespace Cane360.Application.Inventory;

public sealed record StockAdjustmentDto(Guid Id, Guid StoreId, Guid StockPositionId, Guid? StockCountLineId,
    string AdjustmentType, string Status, string ItemCode, string ItemName, string? LotCode, string UnitCode,
    decimal SignedQuantity, decimal? ExplicitUnitValueUsd, decimal? UnitCostUsdSnapshot, decimal? SignedValueUsdSnapshot,
    string Reason, DateOnly EventDate, long Version, Guid? StockMovementId, Guid? ReversalOfStockAdjustmentId,
    Guid? ReversalStockAdjustmentId, string? CancellationReason);
