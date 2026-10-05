namespace Cane360.Application.Inventory;

public sealed class GetStockMovementsQueryHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetStockMovementsQuery, IReadOnlyList<StockMovementDto>>
{
    public async Task<IReadOnlyList<StockMovementDto>> Handle(
        GetStockMovementsQuery request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        return (await inventoryRepository.GetMovementsAsync(
            tenant.Id, farm.Id, request.ItemId, cancellationToken)).Select(InventoryMapper.Movement).ToArray();
    }
}
