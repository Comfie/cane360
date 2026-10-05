namespace Cane360.Application.Inventory;

public sealed record SubmitOpeningBalanceCommand(
    Guid ReceiptId,
    long ExpectedVersion) : IRequest<StockReceiptDto>;
