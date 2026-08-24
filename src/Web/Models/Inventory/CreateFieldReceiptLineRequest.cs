namespace Cane360.Web.Models.Inventory;

public sealed record CreateFieldReceiptLineRequest(Guid StockIssueLineId, decimal Quantity);
