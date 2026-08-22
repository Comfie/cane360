using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

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
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var unit = await inventoryRepository.GetUnitAsync(tenant.Id, request.StockUnitId, false, cancellationToken)
            ?? throw new NotFoundException(request.StockUnitId.ToString(), "Unit of measure");
        var item = InventoryAccess.ApplyDomainAction(nameof(request.StockUnitId), () => InventoryItem.Create(
            tenant.Id,
            farm.Id,
            request.Code,
            request.Name,
            Enum.Parse<InventoryItemCategory>(request.Category, true),
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
