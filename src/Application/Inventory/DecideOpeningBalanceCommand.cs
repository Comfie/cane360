using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record DecideOpeningBalanceCommand(
    Guid ReceiptId,
    long ExpectedVersion,
    string Outcome,
    string? Reason,
    string IdempotencyKey) : IRequest<StockReceiptDto>;
