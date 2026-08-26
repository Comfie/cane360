namespace Cane360.Application.Inventory;

public sealed class GetStockCountsQueryHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetStockCountsQuery, IReadOnlyList<StockCountDto>>
{
    public async Task<IReadOnlyList<StockCountDto>> Handle(GetStockCountsQuery query, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant);
        return (await inventoryRepository.GetStockCountsAsync(tenant.Id, farm.Id, false, cancellationToken)).Select(InventoryMapper.Count).ToArray();
    }
}
