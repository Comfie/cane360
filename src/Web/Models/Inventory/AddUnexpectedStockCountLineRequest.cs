namespace Cane360.Web.Models.Inventory;

public sealed record AddUnexpectedStockCountLineRequest(Guid InventoryItemId, Guid? InventoryLotId, long ExpectedCountVersion);
