namespace Cane360.Application.Inventory;

public sealed record ReverseStockIssueCommand(
    Guid StockIssueId, long ExpectedVersion, string Reason, string IdempotencyKey) : IRequest;
