namespace Cane360.Application.Inventory;

public sealed record StockCountLineDto(Guid Id, Guid StockPositionId, Guid InventoryItemId, Guid? InventoryLotId,
    string ItemCode, string ItemName, string? LotCode, string UnitCode, decimal ExpectedQuantity,
    decimal ExpectedValueUsd, decimal? CountedQuantity, decimal VarianceQuantity, string? Notes,
    DateTimeOffset? EnteredAt, long Version, Guid? PostedStockAdjustmentId);
