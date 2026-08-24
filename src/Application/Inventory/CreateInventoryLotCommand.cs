using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed record CreateInventoryLotCommand(
    Guid InventoryItemId, string Code, DateOnly? ExpiryDate) : IRequest<InventoryLotDto>;
