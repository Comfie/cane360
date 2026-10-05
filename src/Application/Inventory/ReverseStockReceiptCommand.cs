using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record ReverseStockReceiptCommand(
    Guid ReceiptId,
    long ExpectedVersion,
    string Reason,
    string IdempotencyKey) : IRequest<StockReceiptDto>;
