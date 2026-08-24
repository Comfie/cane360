namespace Cane360.Web.Models.Inventory;

public sealed record CreateInventoryItemRequest(
    string Code,
    string Name,
    string Category,
    Guid StockUnitId,
    decimal? ReorderLevel,
    string LotTrackingPolicy,
    string ExpiryPolicy);
