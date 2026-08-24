namespace Cane360.Web.Models.Inventory;

public sealed record CreateInputRequestLineRequest(Guid InventoryItemId, decimal RequestedQuantity);
