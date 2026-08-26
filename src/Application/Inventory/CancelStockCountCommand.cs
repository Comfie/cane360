namespace Cane360.Application.Inventory;

public sealed record CancelStockCountCommand(Guid StockCountId, long ExpectedVersion, string Reason) : IRequest<StockCountDto>;
