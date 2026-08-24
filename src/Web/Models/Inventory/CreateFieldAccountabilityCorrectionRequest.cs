namespace Cane360.Web.Models.Inventory;

public sealed record CreateFieldAccountabilityCorrectionRequest(
    Guid? FieldReceiptId,
    Guid? InputApplicationId,
    Guid? StockReturnId,
    Guid? InventoryLossId,
    long SourceVersion,
    string Reason,
    string IdempotencyKey);
