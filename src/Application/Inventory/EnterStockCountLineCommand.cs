namespace Cane360.Application.Inventory;

public sealed record EnterStockCountLineCommand(Guid StockCountId, Guid StockCountLineId, decimal CountedQuantity,
    string? Notes, long ExpectedVersion) : IRequest<StockCountDto>;
