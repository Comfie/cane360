namespace Cane360.Web.Models.Inventory;

public sealed record ReverseStockReceiptRequest(
    long ExpectedVersion, string Reason, string IdempotencyKey);
