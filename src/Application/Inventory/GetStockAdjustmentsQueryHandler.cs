namespace Cane360.Application.Inventory;

public sealed class GetStockAdjustmentsQueryHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetStockAdjustmentsQuery, IReadOnlyList<StockAdjustmentDto>>
{
    public async Task<IReadOnlyList<StockAdjustmentDto>> Handle(GetStockAdjustmentsQuery query, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant);
        return (await inventoryRepository.GetStockAdjustmentsAsync(tenant.Id, farm.Id, false, cancellationToken)).Select(InventoryMapper.Adjustment).ToArray();
    }
}
