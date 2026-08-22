using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateInventoryLotCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateInventoryLotCommand, InventoryLotDto>
{
    public async Task<InventoryLotDto> Handle(
        CreateInventoryLotCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var item = await inventoryRepository.GetItemAsync(
            tenant.Id, farm.Id, request.InventoryItemId, false, cancellationToken)
            ?? throw new NotFoundException(request.InventoryItemId.ToString(), "Inventory item");
        var lot = InventoryAccess.ApplyDomainAction(nameof(request.Code), () =>
            InventoryLot.Create(tenant.Id, farm.Id, item, request.Code, request.ExpiryDate));
        inventoryRepository.Add(lot);
        inventoryRepository.Add(StockPosition.Create(tenant.Id, farm.Id, farm.Store.Id, item.Id, lot.Id));
        InventoryAudit.Lot(inventoryRepository, tenant, farm, user, lot, "Created", timeProvider.GetUtcNow(),
            $"Created lot {lot.Code} for inventory item {item.Code}.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return InventoryMapper.Lot(lot);
    }
}
