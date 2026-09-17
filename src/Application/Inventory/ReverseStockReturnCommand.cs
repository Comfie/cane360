using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record ReverseStockReturnCommand(Guid StockReturnId, long ExpectedVersion, string Reason,
    string IdempotencyKey) : IRequest;
