using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record SubmitStockAdjustmentCommand(Guid StockAdjustmentId, long ExpectedVersion) : IRequest<StockAdjustmentDto>;
