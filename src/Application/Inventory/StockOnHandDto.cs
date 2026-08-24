namespace Cane360.Application.Inventory;

public sealed record StockOnHandDto(
    Guid StockPositionId,
    Guid InventoryItemId,
    Guid? InventoryLotId,
    string ItemCode,
    string ItemName,
    string? LotCode,
    string UnitCode,
    decimal Quantity,
    decimal StockValueUsd,
    decimal WeightedAverageUnitCostUsd,
    decimal? ReorderLevel);
