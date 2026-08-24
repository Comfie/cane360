namespace Cane360.Application.Inventory;

public sealed record CreateFieldReceiptLineCommand(Guid StockIssueLineId, decimal Quantity);
