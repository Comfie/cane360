using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class SubmitStockAdjustmentCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<SubmitStockAdjustmentCommand, StockAdjustmentDto>
{
    public async Task<StockAdjustmentDto> Handle(SubmitStockAdjustmentCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireFarmManager(tenant, userId);
        var adjustment = await inventoryRepository.GetStockAdjustmentAsync(tenant.Id, farm.Id, command.StockAdjustmentId, true, cancellationToken) ?? throw new NotFoundException(command.StockAdjustmentId.ToString(), "Stock adjustment");
        if (adjustment.Version != command.ExpectedVersion) throw new ConflictException("This adjustment changed after it was loaded. Refresh and try again."); var now = timeProvider.GetUtcNow(); InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => adjustment.Submit(now, command.ExpectedVersion));
        InventoryAudit.Adjustment(inventoryRepository, tenant, farm, user, adjustment, "Submitted", now, null, "Submitted adjustment for exact-version Grower approval."); await inventoryRepository.SaveChangesAsync(cancellationToken); return InventoryMapper.Adjustment(adjustment);
    }
}
