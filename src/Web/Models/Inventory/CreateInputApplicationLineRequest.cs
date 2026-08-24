namespace Cane360.Web.Models.Inventory;

public sealed record CreateInputApplicationLineRequest(Guid FieldReceiptLineId, Guid StockIssueLineId, decimal AppliedQuantity);
