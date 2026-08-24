namespace Cane360.Web.Models.Inventory;

public sealed record CreateStockReturnLineRequest(Guid StockIssueLineId, decimal Quantity);
