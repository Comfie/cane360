namespace Cane360.Web.Models.Inventory;

public sealed record CreateStockReceiptRequest(
    string ReceiptType,
    Guid? SupplierId,
    string ReceiptDate,
    Guid? ReceivedByPersonId,
    string SourceReference,
    string? Reason,
    string? LateEntryReason,
    IReadOnlyList<CreateStockReceiptLineRequest> Lines);
