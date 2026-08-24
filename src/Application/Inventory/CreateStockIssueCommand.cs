namespace Cane360.Application.Inventory;

public sealed record CreateStockIssueCommand(
    Guid InputRequestId, DateOnly IssueDate, Guid IssuerPersonId,
    Guid RecipientPersonId, string? LateEntryReason,
    IReadOnlyList<CreateStockIssueLineCommand> Lines) : IRequest<Guid>;
