namespace Cane360.Web.Models.Inventory;

public sealed record CreateStockIssueRequest(
    Guid InputRequestId, DateOnly IssueDate, Guid IssuerPersonId,
    Guid RecipientPersonId, string? LateEntryReason,
    IReadOnlyList<CreateStockIssueLineRequest> Lines);
