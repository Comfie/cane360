using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CancelStockCountCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<CancelStockCountCommand, StockCountDto>
{
    public async Task<StockCountDto> Handle(CancelStockCountCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var candidate = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, command.StockCountId, false, cancellationToken) ?? throw new NotFoundException(command.StockCountId.ToString(), "Stock count");
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken); await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken); await inventoryRepository.LockStockCountAsync(tenant.Id, farm.Id, candidate.Id, cancellationToken);
        var count = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Stock count");
        if (count.Version != command.ExpectedVersion) throw new ConflictException("This count changed after it was loaded. Refresh and try again.");
        if (count.Lines.Any(line => line.PostedStockAdjustmentId.HasValue)) throw new ConflictException("A count cannot be cancelled after an adjustment has posted.");
        var now = timeProvider.GetUtcNow(); InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => count.Cancel(command.Reason, command.ExpectedVersion));
        InventoryAudit.Count(inventoryRepository, tenant, farm, user, count, "Cancelled", now, command.Reason, "Cancelled physical count and released any Store posting freeze.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return InventoryMapper.Count(count);
    }
}
