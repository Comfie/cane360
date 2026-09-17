using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CancelStockCountCommand(Guid StockCountId, long ExpectedVersion, string Reason) : IRequest<StockCountDto>;
