using Cane360.Application.Common.Security;
using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record ReverseStockReceiptCommand(
    Guid ReceiptId,
    long ExpectedVersion,
    string Reason,
    string IdempotencyKey) : IRequest<StockReceiptDto>;
