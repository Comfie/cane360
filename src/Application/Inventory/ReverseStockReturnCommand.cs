namespace Cane360.Application.Inventory;

public sealed record ReverseStockReturnCommand(Guid StockReturnId, long ExpectedVersion, string Reason,
    string IdempotencyKey) : IRequest;
