namespace Cane360.Application.Inventory;

public sealed record InventoryItemDto(
    Guid Id,
    string Code,
    string Name,
    string Category,
    Guid StockUnitId,
    string StockUnitCode,
    decimal? ReorderLevel,
    string LotTrackingPolicy,
    string ExpiryPolicy,
    string CostingMethod,
    string Status,
    long Version);
