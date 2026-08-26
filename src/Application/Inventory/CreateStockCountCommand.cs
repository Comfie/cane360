namespace Cane360.Application.Inventory;

public sealed record CreateStockCountCommand(DateOnly EventDate, string Notes, string CountingPersons) : IRequest<StockCountDto>;
