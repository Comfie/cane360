namespace Cane360.Application.Inventory;

public sealed record GetStockReceiptQuery(Guid ReceiptId) : IRequest<StockReceiptDto>;
