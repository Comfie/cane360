namespace Cane360.Web.Models.Inventory;

public sealed record CreateStockIssueLineRequest(
    Guid InputRequestLineId, Guid? InventoryLotId, decimal Quantity);
