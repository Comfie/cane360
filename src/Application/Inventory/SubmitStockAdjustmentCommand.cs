namespace Cane360.Application.Inventory;

public sealed record SubmitStockAdjustmentCommand(Guid StockAdjustmentId, long ExpectedVersion) : IRequest<StockAdjustmentDto>;
