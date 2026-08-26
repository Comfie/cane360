using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateStockAdjustmentCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<CreateStockAdjustmentCommand, StockAdjustmentDto>
{
    public async Task<StockAdjustmentDto> Handle(CreateStockAdjustmentCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        if (!Enum.TryParse<StockAdjustmentType>(command.AdjustmentType, true, out var type)) throw InventoryAccess.Failure(nameof(command.AdjustmentType), "Unknown stock adjustment type.");
        StockCountLine? countLine = null; StockCount? count = null; StockPosition position; InventoryItem item; InventoryLot? lot; UnitOfMeasure unit; decimal signedQuantity;
        if (command.StockCountLineId.HasValue)
        {
            countLine = await inventoryRepository.GetStockCountLineAsync(tenant.Id, farm.Id, command.StockCountLineId.Value, false, cancellationToken) ?? throw new NotFoundException(command.StockCountLineId.Value.ToString(), "Stock count line");
            count = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, countLine.StockCountId, false, cancellationToken) ?? throw new NotFoundException(countLine.StockCountId.ToString(), "Stock count");
            if (count.Status != StockCountStatus.PendingAdjustment || countLine.IsResolved || countLine.VarianceQuantity == 0) throw new ConflictException("Only an unresolved reviewed variance can create a count adjustment.");
            if (type != StockAdjustmentType.CountVariance) throw InventoryAccess.Failure(nameof(command.AdjustmentType), "Count-derived adjustments must use CountVariance.");
            signedQuantity = countLine.VarianceQuantity;
            if (command.SignedQuantity.HasValue && command.SignedQuantity.Value != signedQuantity) throw InventoryAccess.Failure(nameof(command.SignedQuantity), "Count adjustment quantity must equal the unresolved variance.");
            position = await inventoryRepository.GetPositionAsync(tenant.Id, farm.Id, count.StoreId, countLine.InventoryItemId, countLine.InventoryLotId, false, cancellationToken) ?? throw new NotFoundException(countLine.StockPositionId.ToString(), "Stock position");
            item = await inventoryRepository.GetItemAsync(tenant.Id, farm.Id, countLine.InventoryItemId, false, cancellationToken) ?? throw new NotFoundException(countLine.InventoryItemId.ToString(), "Inventory item");
            lot = countLine.InventoryLotId.HasValue ? await inventoryRepository.GetLotAsync(tenant.Id, farm.Id, countLine.InventoryLotId.Value, false, cancellationToken) : null;
            unit = await inventoryRepository.GetUnitAsync(tenant.Id, item.StockUnitId, false, cancellationToken) ?? throw new NotFoundException(item.StockUnitId.ToString(), "Stock unit");
        }
        else
        {
            if (!command.InventoryItemId.HasValue || !command.SignedQuantity.HasValue) throw InventoryAccess.Failure(nameof(command.InventoryItemId), "Store adjustments require an item and signed quantity.");
            signedQuantity = command.SignedQuantity.Value; item = await inventoryRepository.GetItemAsync(tenant.Id, farm.Id, command.InventoryItemId.Value, false, cancellationToken) ?? throw new NotFoundException(command.InventoryItemId.Value.ToString(), "Inventory item");
            lot = command.InventoryLotId.HasValue ? await inventoryRepository.GetLotAsync(tenant.Id, farm.Id, command.InventoryLotId.Value, false, cancellationToken) : null;
            if (command.InventoryLotId.HasValue && lot?.InventoryItemId != item.Id)
                throw new NotFoundException(command.InventoryLotId.Value.ToString(), "Inventory lot");
            position = await inventoryRepository.GetPositionAsync(tenant.Id, farm.Id, farm.Store.Id, item.Id, lot?.Id, false, cancellationToken) ?? StockPosition.Create(tenant.Id, farm.Id, farm.Store.Id, item.Id, lot?.Id);
            if (await inventoryRepository.GetPositionAsync(tenant.Id, farm.Id, farm.Store.Id, item.Id, lot?.Id, false, cancellationToken) is null) inventoryRepository.Add(position);
            unit = await inventoryRepository.GetUnitAsync(tenant.Id, item.StockUnitId, false, cancellationToken) ?? throw new NotFoundException(item.StockUnitId.ToString(), "Stock unit");
            if (type == StockAdjustmentType.CountVariance || (type == StockAdjustmentType.PositiveCorrection && signedQuantity <= 0) || (type is StockAdjustmentType.Damaged or StockAdjustmentType.Expired or StockAdjustmentType.Spilled or StockAdjustmentType.UnexplainedWriteOff && signedQuantity >= 0)) throw InventoryAccess.Failure(nameof(command.SignedQuantity), "The signed quantity does not match the selected adjustment type.");
        }
        var adjustment = InventoryAccess.ApplyDomainAction(nameof(command.SignedQuantity), () => StockAdjustment.Create(tenant.Id, farm.Id, count?.StoreId ?? farm.Store.Id, position, item, lot, unit, countLine?.Id, type, signedQuantity, command.ExplicitUnitValueUsd, countLine?.Version, count?.Version, command.Reason, command.EventDate, userId));
        inventoryRepository.Add(adjustment); InventoryAudit.Adjustment(inventoryRepository, tenant, farm, user, adjustment, "Created", timeProvider.GetUtcNow(), command.Reason, "Created draft signed store adjustment; Grower approval is required before posting.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); return InventoryMapper.Adjustment(adjustment);
    }
}
