namespace Cane360.Application.Inventory;

public sealed record AddUnexpectedStockCountLineCommand(Guid StockCountId, Guid InventoryItemId, Guid? InventoryLotId,
    long ExpectedCountVersion) : IRequest<StockCountDto>;
