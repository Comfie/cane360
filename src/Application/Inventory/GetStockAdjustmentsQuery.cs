namespace Cane360.Application.Inventory;

public sealed record GetStockAdjustmentsQuery : IRequest<IReadOnlyList<StockAdjustmentDto>>;
