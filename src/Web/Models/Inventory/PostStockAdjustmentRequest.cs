namespace Cane360.Web.Models.Inventory;

public sealed record PostStockAdjustmentRequest(long ExpectedVersion, string IdempotencyKey);
