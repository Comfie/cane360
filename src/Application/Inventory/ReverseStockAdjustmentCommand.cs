using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record ReverseStockAdjustmentCommand(Guid StockAdjustmentId, string Reason, string IdempotencyKey) : IRequest<StockAdjustmentDto>;
