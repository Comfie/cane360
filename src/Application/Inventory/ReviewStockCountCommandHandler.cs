using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class ReviewStockCountCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<ReviewStockCountCommand, StockCountDto>
{
    public async Task<StockCountDto> Handle(ReviewStockCountCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var candidate = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, command.StockCountId, false, cancellationToken) ?? throw new NotFoundException(command.StockCountId.ToString(), "Stock count");
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken); await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken); await inventoryRepository.LockStockCountAsync(tenant.Id, farm.Id, candidate.Id, cancellationToken);
        var count = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Stock count");
        if (count.Version != command.ExpectedVersion) throw new ConflictException("This count changed after it was loaded. Refresh and try again.");
        if (count.Lines.Any(line => !line.CountedQuantity.HasValue)) throw new ConflictException("Every count line must have a physical quantity before review.");
        var now = timeProvider.GetUtcNow(); InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => count.MoveToReview(now, command.ExpectedVersion));
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => count.ResolveReview(now, count.Version));
        InventoryAudit.Count(inventoryRepository, tenant, farm, user, count, "Reviewed", now, null, count.Status == StockCountStatus.ClosedNoVariance ? "Count closed with no variance and Store freeze released." : "Count is awaiting Grower-approved signed adjustments; Store freeze released.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return InventoryMapper.Count(count);
    }
}
