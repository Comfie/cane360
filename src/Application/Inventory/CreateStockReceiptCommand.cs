namespace Cane360.Application.Inventory;

public sealed record CreateStockReceiptCommand(
    string ReceiptType,
    Guid? SupplierId,
    DateOnly ReceiptDate,
    Guid? ReceivedByPersonId,
    string SourceReference,
    string? Reason,
    string? LateEntryReason,
    IReadOnlyList<CreateStockReceiptLineCommand> Lines) : IRequest<StockReceiptDto>;
