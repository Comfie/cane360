namespace Cane360.Application.Inventory;

public sealed class CreateStockCountCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateStockCountCommand, StockCountDto>
{
    public async Task<StockCountDto> Handle(CreateStockCountCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);

        StockCount count = InventoryAccess.ApplyDomainAction(nameof(command.CountingPersons),
            () => StockCount.Create(tenant.Id, farm.Id, farm.Store.Id, command.Notes, command.CountingPersons,
                command.EventDate, userId));
        inventoryRepository.Add(count);
        InventoryAudit.Count(inventoryRepository, tenant, farm, user, count, "Created", timeProvider.GetUtcNow(), null,
            "Created a draft full-store physical count.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Count(count);
    }
}
