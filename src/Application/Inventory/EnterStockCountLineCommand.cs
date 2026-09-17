using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record EnterStockCountLineCommand(Guid StockCountId, Guid StockCountLineId, decimal CountedQuantity,
    string? Notes, long ExpectedVersion) : IRequest<StockCountDto>;
