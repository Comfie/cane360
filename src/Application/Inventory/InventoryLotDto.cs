namespace Cane360.Application.Inventory;

public sealed record InventoryLotDto(
    Guid Id, Guid InventoryItemId, string Code, DateOnly? ExpiryDate, string Status, long Version);
