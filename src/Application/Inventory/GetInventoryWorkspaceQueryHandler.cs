namespace Cane360.Application.Inventory;

public sealed class GetInventoryWorkspaceQueryHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user) : IRequestHandler<GetInventoryWorkspaceQuery, InventoryWorkspaceDto>
{
    public async Task<InventoryWorkspaceDto> Handle(
        GetInventoryWorkspaceQuery request, CancellationToken cancellationToken)
    {
        string userId = InventoryAccess.RequireUserId(user);
        Tenant tenant = await farmRepository.GetTenantPeopleContextForUserAsync(userId, false,
            cancellationToken) ?? throw new NotFoundException(userId,
            "Active grower or farm-manager membership");
        Farm farm = InventoryAccess.RequireFarm(tenant);
        IReadOnlyList<UnitOfMeasure> units =
            await inventoryRepository.GetUnitsAsync(tenant.Id, false, cancellationToken);
        IReadOnlyList<InventoryItem> items =
            await inventoryRepository.GetItemsAsync(tenant.Id, farm.Id, false, cancellationToken);
        IReadOnlyList<Supplier> suppliers =
            await inventoryRepository.GetSuppliersAsync(tenant.Id, farm.Id, false, cancellationToken);
        IReadOnlyList<InventoryLot> lots =
            await inventoryRepository.GetLotsAsync(tenant.Id, farm.Id, null, false, cancellationToken);
        IReadOnlyList<StockReceipt> receipts =
            await inventoryRepository.GetReceiptsAsync(tenant.Id, farm.Id, false, cancellationToken);
        IReadOnlyList<(StockPosition Position, StockLedgerSnapshot Snapshot)> stock =
            await inventoryRepository.GetStockOnHandAsync(tenant.Id, farm.Id, cancellationToken);
        IReadOnlyList<StockMovement> movements =
            await inventoryRepository.GetMovementsAsync(tenant.Id, farm.Id, null, cancellationToken);
        IReadOnlyList<InventoryCategory> categories =
            await inventoryRepository.GetCategoriesAsync(tenant.Id, false, cancellationToken);
        bool canManageCategories = InventoryAccess.SecurityRole(tenant, userId) == TenantSecurityRoles.FarmManager;
        return InventoryMapper.Workspace(tenant, farm, units, items, suppliers, lots, receipts, stock, movements,
            categories, canManageCategories);
    }
}
