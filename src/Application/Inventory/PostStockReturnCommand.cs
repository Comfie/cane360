namespace Cane360.Application.Inventory;

public sealed record PostStockReturnCommand(Guid StockReturnId, long ExpectedVersion, string IdempotencyKey) : IRequest;
