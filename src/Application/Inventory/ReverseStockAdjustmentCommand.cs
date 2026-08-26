namespace Cane360.Application.Inventory;

public sealed record ReverseStockAdjustmentCommand(Guid StockAdjustmentId, string Reason, string IdempotencyKey) : IRequest<StockAdjustmentDto>;
