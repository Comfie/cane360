namespace Cane360.Application.Inventory;

public sealed record StartStockCountCommand(Guid StockCountId, long ExpectedVersion) : IRequest<StockCountDto>;
