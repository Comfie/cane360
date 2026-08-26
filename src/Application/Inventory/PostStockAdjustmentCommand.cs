namespace Cane360.Application.Inventory;

public sealed record PostStockAdjustmentCommand(Guid StockAdjustmentId, long ExpectedVersion, string IdempotencyKey) : IRequest<StockAdjustmentDto>;
