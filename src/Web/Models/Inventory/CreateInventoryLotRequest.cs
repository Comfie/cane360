namespace Cane360.Web.Models.Inventory;

public sealed record CreateInventoryLotRequest(
    Guid InventoryItemId, string Code, string? ExpiryDate);
