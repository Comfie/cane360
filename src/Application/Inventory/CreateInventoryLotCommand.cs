namespace Cane360.Application.Inventory;

public sealed record CreateInventoryLotCommand(
    Guid InventoryItemId,
    string Code,
    DateOnly? ExpiryDate) : IRequest<InventoryLotDto>;
