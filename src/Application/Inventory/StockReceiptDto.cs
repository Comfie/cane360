namespace Cane360.Application.Inventory;

public sealed record StockReceiptDto(
    Guid Id,
    string ReceiptType,
    Guid? SupplierId,
    string? SupplierName,
    DateOnly ReceiptDate,
    Guid? ReceivedByPersonId,
    string? ReceivedByPersonName,
    string SourceReference,
    string? Reason,
    string? LateEntryReason,
    string Status,
    DateTimeOffset? PostedAt,
    DateTimeOffset? ReversedAt,
    long Version,
    decimal TotalValueUsd,
    IReadOnlyList<StockReceiptLineDto> Lines);
