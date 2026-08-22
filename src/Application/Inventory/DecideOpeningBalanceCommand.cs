using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed record DecideOpeningBalanceCommand(
    Guid ReceiptId,
    long ExpectedVersion,
    string Outcome,
    string? Reason,
    string IdempotencyKey) : IRequest<StockReceiptDto>;
