namespace Cane360.Application.Inventory;

public sealed record CreateInventoryItemCommand(
    string Code,
    string Name,
    string Category,
    Guid StockUnitId,
    decimal? ReorderLevel,
    string LotTrackingPolicy,
    string ExpiryPolicy) : IRequest<InventoryItemDto>;
