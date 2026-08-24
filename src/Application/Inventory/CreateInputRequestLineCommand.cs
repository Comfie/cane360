namespace Cane360.Application.Inventory;

public sealed record CreateInputRequestLineCommand(Guid InventoryItemId, decimal RequestedQuantity);
