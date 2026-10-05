namespace Cane360.Application.Inventory;

public sealed record GetStockMovementsQuery(Guid? ItemId) : IRequest<IReadOnlyList<StockMovementDto>>;
