using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed record CreateInventoryItemCommand(
    string Code,
    string Name,
    string Category,
    Guid StockUnitId,
    decimal? ReorderLevel,
    string LotTrackingPolicy,
    string ExpiryPolicy) : IRequest<InventoryItemDto>;
