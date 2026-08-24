namespace Cane360.Web.Models.Inventory;

public sealed record PostStockReceiptRequest(long ExpectedVersion, string IdempotencyKey);
