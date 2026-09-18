using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record AddUnexpectedStockCountLineCommand(Guid StockCountId, Guid InventoryItemId, Guid? InventoryLotId,
    long ExpectedCountVersion) : IRequest<StockCountDto>;
