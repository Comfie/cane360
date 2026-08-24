namespace Cane360.Application.Inventory;

public sealed record PostStockIssueCommand(
    Guid StockIssueId, long ExpectedVersion, string IdempotencyKey) : IRequest;
