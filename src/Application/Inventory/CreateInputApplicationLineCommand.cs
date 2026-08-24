namespace Cane360.Application.Inventory;

public sealed record CreateInputApplicationLineCommand(Guid FieldReceiptLineId, Guid StockIssueLineId, decimal AppliedQuantity);
