using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class GetInventoryWorkspaceQueryHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetInventoryWorkspaceQuery, InventoryWorkspaceDto>
{
    public async Task<InventoryWorkspaceDto> Handle(
        GetInventoryWorkspaceQuery request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var units = await inventoryRepository.GetUnitsAsync(tenant.Id, false, cancellationToken);
        var items = await inventoryRepository.GetItemsAsync(tenant.Id, farm.Id, false, cancellationToken);
        var suppliers = await inventoryRepository.GetSuppliersAsync(tenant.Id, farm.Id, false, cancellationToken);
        var lots = await inventoryRepository.GetLotsAsync(tenant.Id, farm.Id, null, false, cancellationToken);
        var receipts = await inventoryRepository.GetReceiptsAsync(tenant.Id, farm.Id, false, cancellationToken);
        var stock = await inventoryRepository.GetStockOnHandAsync(tenant.Id, farm.Id, cancellationToken);
        var movements = await inventoryRepository.GetMovementsAsync(tenant.Id, farm.Id, null, cancellationToken);
        return InventoryMapper.Workspace(tenant, farm, units, items, suppliers, lots, receipts, stock, movements);
    }
}
