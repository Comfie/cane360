namespace Cane360.Application.Inventory;

public sealed record StockIssueDto(
    Guid Id, Guid InputRequestId, DateOnly IssueDate, Guid IssuerPersonId,
    Guid RecipientPersonId, string Status, DateTimeOffset? PostedAt, long Version,
    IReadOnlyList<StockIssueLineDto> Lines);
