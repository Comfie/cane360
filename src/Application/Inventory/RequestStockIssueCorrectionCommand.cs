namespace Cane360.Application.Inventory;

public sealed record RequestStockIssueCorrectionCommand(
    Guid StockIssueId, long ExpectedVersion, string Reason) : IRequest;
