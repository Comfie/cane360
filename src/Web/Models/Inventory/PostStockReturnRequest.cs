namespace Cane360.Web.Models.Inventory;

public sealed record PostStockReturnRequest(long ExpectedVersion, string IdempotencyKey);
