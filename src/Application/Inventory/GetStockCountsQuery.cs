namespace Cane360.Application.Inventory;

public sealed record GetStockCountsQuery : IRequest<IReadOnlyList<StockCountDto>>;
