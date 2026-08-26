namespace Cane360.Web.Models.Inventory;

public sealed record ReverseStockAdjustmentRequest(string Reason, string IdempotencyKey);
