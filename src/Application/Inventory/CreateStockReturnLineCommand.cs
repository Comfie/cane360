namespace Cane360.Application.Inventory;

public sealed record CreateStockReturnLineCommand(Guid StockIssueLineId, decimal Quantity);
