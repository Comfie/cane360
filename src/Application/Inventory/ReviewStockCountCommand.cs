namespace Cane360.Application.Inventory;

public sealed record ReviewStockCountCommand(Guid StockCountId, long ExpectedVersion) : IRequest<StockCountDto>;
