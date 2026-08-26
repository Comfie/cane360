using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class StartStockCountCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<StartStockCountCommand, StockCountDto>
{
    public async Task<StockCountDto> Handle(StartStockCountCommand command, CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                return await StartOnceAsync(command, cancellationToken);
            }
            catch (InventorySerializationFailureException) when (attempt < maximumAttempts)
            {
                inventoryRepository.ResetTrackedChanges();
            }
            catch (InventorySerializationFailureException)
            {
                throw new ConflictException("Concurrent count start did not settle after three attempts. Retry the command.");
            }
        }

        throw new InvalidOperationException("The stock count start retry loop ended unexpectedly.");
    }

    private async Task<StockCountDto> StartOnceAsync(StartStockCountCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var candidate = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, command.StockCountId, false, cancellationToken) ?? throw new NotFoundException(command.StockCountId.ToString(), "Stock count");
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken);
        if (await inventoryRepository.GetActiveStockCountAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken) is not null) throw new ConflictException("Another full-store count is already in progress.");
        await inventoryRepository.LockStockCountAsync(tenant.Id, farm.Id, candidate.Id, cancellationToken);
        var count = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Stock count");
        if (count.Version != command.ExpectedVersion) throw new ConflictException("This count changed after it was loaded. Refresh and try again.");
        var cutoff = await inventoryRepository.GetHighestPostingSequenceAsync(tenant.Id, farm.Id, count.StoreId, cancellationToken);
        var snapshots = await inventoryRepository.GetNonZeroStockAtCutoffAsync(tenant.Id, farm.Id, count.StoreId, cutoff, cancellationToken); var lines = new List<StockCountLine>();
        foreach (var (position, snapshot) in snapshots)
        {
            var item = await inventoryRepository.GetItemAsync(tenant.Id, farm.Id, position.InventoryItemId, false, cancellationToken) ?? throw new NotFoundException(position.InventoryItemId.ToString(), "Inventory item");
            var unit = await inventoryRepository.GetUnitAsync(tenant.Id, item.StockUnitId, false, cancellationToken) ?? throw new NotFoundException(item.StockUnitId.ToString(), "Stock unit");
            var lot = position.InventoryLotId.HasValue ? await inventoryRepository.GetLotAsync(tenant.Id, farm.Id, position.InventoryLotId.Value, false, cancellationToken) : null;
            lines.Add(StockCountLine.Create(count, position, item, lot, unit, snapshot.Quantity, snapshot.ValueUsd));
        }
        var now = timeProvider.GetUtcNow(); InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => count.Start(cutoff, lines, now, command.ExpectedVersion));
        InventoryAudit.Count(inventoryRepository, tenant, farm, user, count, "Started", now, null, $"Started full-store count at posting cut-off {cutoff} and froze Store postings.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return InventoryMapper.Count(count);
    }
}
