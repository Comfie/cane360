namespace Cane360.Application.Inventory;

public sealed class AddUnexpectedStockCountLineCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<AddUnexpectedStockCountLineCommand, StockCountDto>
{
    public async Task<StockCountDto> Handle(AddUnexpectedStockCountLineCommand command,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);
        StockCount count =
            await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, command.StockCountId, true,
                cancellationToken) ?? throw new NotFoundException(command.StockCountId.ToString(), "Stock count");
        if (count.Status != StockCountStatus.InProgress || count.Version != command.ExpectedCountVersion)
        {
            throw new ConflictException("Only the current in-progress count accepts unexpected stock.");
        }

        InventoryItem item =
            await inventoryRepository.GetItemAsync(tenant.Id, farm.Id, command.InventoryItemId, false,
                cancellationToken) ?? throw new NotFoundException(command.InventoryItemId.ToString(), "Inventory item");
        InventoryLot? lot = command.InventoryLotId.HasValue
            ? await inventoryRepository.GetLotAsync(tenant.Id, farm.Id, command.InventoryLotId.Value, false,
                cancellationToken)
            : null;
        if (lot?.InventoryItemId != item.Id)
        {
            throw new NotFoundException(command.InventoryLotId!.Value.ToString(), "Inventory lot");
        }

        StockPosition? position = await inventoryRepository.GetPositionAsync(tenant.Id, farm.Id, count.StoreId, item.Id,
            lot?.Id, true, cancellationToken);
        if (position is null)
        {
            position = StockPosition.Create(tenant.Id, farm.Id, count.StoreId, item.Id, lot?.Id);
            inventoryRepository.Add(position);
        }

        if (count.Lines.Any(line => line.StockPositionId == position.Id))
        {
            throw new ConflictException("That item and lot are already in this count.");
        }

        UnitOfMeasure unit =
            await inventoryRepository.GetUnitAsync(tenant.Id, item.StockUnitId, false, cancellationToken) ??
            throw new NotFoundException(item.StockUnitId.ToString(), "Stock unit");
        count.Lines.Add(StockCountLine.Create(count, position, item, lot, unit, 0, 0));
        InventoryAudit.Count(inventoryRepository, tenant, farm, user, count, "UnexpectedPositionAdded",
            timeProvider.GetUtcNow(), null, "Added an unexpected physical item or lot with zero expected stock.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Count(count);
    }
}
