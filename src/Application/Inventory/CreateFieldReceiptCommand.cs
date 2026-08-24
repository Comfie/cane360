namespace Cane360.Application.Inventory;

public sealed record CreateFieldReceiptCommand(Guid StockIssueId, Guid FieldId, Guid CropCycleId, Guid ActivityId,
    Guid RecipientPersonId, DateTimeOffset ReceivedAt, string? LateEntryReason,
    IReadOnlyList<CreateFieldReceiptLineCommand> Lines) : IRequest<Guid>;
