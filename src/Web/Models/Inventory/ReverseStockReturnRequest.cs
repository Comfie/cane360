namespace Cane360.Web.Models.Inventory;

public sealed record ReverseStockReturnRequest(long ExpectedVersion, string Reason, string IdempotencyKey);
