using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class GetStockMovementsQueryHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    public async Task<IReadOnlyList<StockMovementDto>> Handle(
        GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        return (await inventoryRepository.GetMovementsAsync(
            tenant.Id, farm.Id, request.ItemId, cancellationToken)).Select(InventoryMapper.Movement).ToArray();
    }
}
