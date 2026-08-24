namespace Cane360.Web.Models.Inventory;

public sealed record CreateFieldReceiptRequest(Guid StockIssueId, Guid FieldId, Guid CropCycleId,
    Guid ActivityId, Guid RecipientPersonId, DateTimeOffset ReceivedAt, string? LateEntryReason,
    IReadOnlyList<CreateFieldReceiptLineRequest> Lines);
