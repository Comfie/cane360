namespace Cane360.Application.Inventory;

public sealed record CreateFieldAccountabilityCorrectionCommand(
    Guid? FieldReceiptId,
    Guid? InputApplicationId,
    Guid? StockReturnId,
    Guid? InventoryLossId,
    long SourceVersion,
    string Reason,
    string IdempotencyKey) : IRequest<Guid>;
