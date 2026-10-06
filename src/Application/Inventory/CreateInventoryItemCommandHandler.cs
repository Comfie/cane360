namespace Cane360.Application.Inventory;

public sealed class CreateInventoryItemCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateInventoryItemCommand, InventoryItemDto>
{
    public async Task<InventoryItemDto> Handle(
        CreateInventoryItemCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        UnitOfMeasure unit =
            await inventoryRepository.GetUnitAsync(tenant.Id, request.StockUnitId, false, cancellationToken)
            ?? throw new NotFoundException(request.StockUnitId.ToString(), "Unit of measure");
        IReadOnlyList<InventoryCategory> categories =
            await inventoryRepository.GetCategoriesAsync(tenant.Id, false, cancellationToken);
        InventoryCategory category = categories.SingleOrDefault(candidate => candidate.Code == request.Category)
            ?? throw InventoryAccess.Failure(nameof(request.Category), "Select a category belonging to this tenant.");
        InventoryItem item = InventoryAccess.ApplyDomainAction(nameof(request.StockUnitId), () => InventoryItem.Create(
            tenant.Id,
            farm.Id,
            request.Code,
            request.Name,
            category,
            unit,
            request.ReorderLevel,
            Enum.Parse<LotTrackingPolicy>(request.LotTrackingPolicy, true),
            Enum.Parse<ExpiryPolicy>(request.ExpiryPolicy, true)));
        inventoryRepository.Add(item);
        inventoryRepository.Add(StockPosition.Create(tenant.Id, farm.Id, farm.Store.Id, item.Id, null));
        InventoryAudit.Item(inventoryRepository, tenant, farm, user, item, "Created", timeProvider.GetUtcNow(),
            $"Created inventory item {item.Code} in stock unit {item.StockUnitCode}.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Item(item);
    }
}
