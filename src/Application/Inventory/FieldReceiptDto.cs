namespace Cane360.Application.Inventory;

public sealed record FieldReceiptDto(
    Guid Id,
    Guid StockIssueId,
    Guid FieldId,
    Guid CropCycleId,
    Guid ActivityId,
    Guid RecipientPersonId,
    DateTimeOffset ReceivedAt,
    string Status,
    long Version,
    IReadOnlyList<FieldReceiptLineDto> Lines);
