namespace Cane360.Application.Inventory;

public sealed record CreateStockIssueLineCommand(
    Guid InputRequestLineId, Guid? InventoryLotId, decimal Quantity);
