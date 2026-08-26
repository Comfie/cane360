using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class PostStockAdjustmentCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<PostStockAdjustmentCommand, StockAdjustmentDto>
{
    public async Task<StockAdjustmentDto> Handle(PostStockAdjustmentCommand command, CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                return await PostOnceAsync(command, cancellationToken);
            }
            catch (InventorySerializationFailureException) when (attempt < maximumAttempts)
            {
                inventoryRepository.ResetTrackedChanges();
            }
            catch (InventorySerializationFailureException)
            {
                throw new ConflictException("Concurrent adjustment posting did not settle after three attempts. Retry the command.");
            }
        }

        throw new InvalidOperationException("The inventory adjustment posting retry loop ended unexpectedly.");
    }

    private async Task<StockAdjustmentDto> PostOnceAsync(PostStockAdjustmentCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var candidate = await inventoryRepository.GetStockAdjustmentAsync(tenant.Id, farm.Id, command.StockAdjustmentId, false, cancellationToken) ?? throw new NotFoundException(command.StockAdjustmentId.ToString(), "Stock adjustment");
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken); await inventoryRepository.EnsureStorePostingNotFrozenAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken);
        await inventoryRepository.LockStockAdjustmentAsync(tenant.Id, farm.Id, candidate.Id, cancellationToken);
        if (candidate.StockCountLineId.HasValue) { var source = await inventoryRepository.GetStockCountLineAsync(tenant.Id, farm.Id, candidate.StockCountLineId.Value, false, cancellationToken) ?? throw new NotFoundException(candidate.StockCountLineId.Value.ToString(), "Stock count line"); await inventoryRepository.LockStockCountAsync(tenant.Id, farm.Id, source.StockCountId, cancellationToken); }
        await inventoryRepository.LockStockPositionsAsync([candidate.StockPositionId], cancellationToken);
        var adjustment = await inventoryRepository.GetStockAdjustmentAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Stock adjustment");
        if (adjustment.Status == StockAdjustmentStatus.Posted && adjustment.StockMovementId.HasValue) return InventoryMapper.Adjustment(adjustment);
        if (adjustment.Version != command.ExpectedVersion) throw new ConflictException("This adjustment changed after it was loaded. Refresh and try again.");
        var approval = await inventoryRepository.GetStockAdjustmentApprovalAsync(adjustment.Id, adjustment.Version - 1, cancellationToken);
        if (approval is not { Outcome: ApprovalOutcome.Approved }) throw new ConflictException("A Grower approval for this exact adjustment version is required.");
        StockCountLine? countLine = null; StockCount? count = null;
        if (adjustment.StockCountLineId.HasValue)
        {
            countLine = await inventoryRepository.GetStockCountLineAsync(tenant.Id, farm.Id, adjustment.StockCountLineId.Value, true, cancellationToken) ?? throw new NotFoundException(adjustment.StockCountLineId.Value.ToString(), "Stock count line");
            count = await inventoryRepository.GetStockCountAsync(tenant.Id, farm.Id, countLine.StockCountId, true, cancellationToken) ?? throw new NotFoundException(countLine.StockCountId.ToString(), "Stock count");
            if (count.Status != StockCountStatus.PendingAdjustment || countLine.IsResolved || countLine.VarianceQuantity != adjustment.SignedQuantity || countLine.Version != adjustment.SourceCountLineVersion || count.Version != adjustment.SourceCountVersion) throw new ConflictException("The approved adjustment no longer matches the immutable unresolved count source.");
        }
        var snapshot = await inventoryRepository.GetPositionSnapshotAsync(adjustment.StockPositionId, cancellationToken);
        if (adjustment.SignedQuantity < 0 && snapshot.Quantity + adjustment.SignedQuantity < 0) throw new ConflictException("This negative adjustment would make stock negative.");
        var unitCost = snapshot.Quantity == 0 ? adjustment.ExplicitUnitValueUsd : snapshot.ValueUsd / snapshot.Quantity;
        if (unitCost is null) throw new ConflictException("A positive adjustment from zero stock needs an explicit USD unit value in the Grower-approved version.");
        var now = timeProvider.GetUtcNow(); var movementId = Guid.NewGuid(); InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => adjustment.Post(unitCost.Value, now, movementId, command.ExpectedVersion));
        var movement = StockMovement.CreateAdjustment(adjustment, now, userId, $"adjustment:{adjustment.Id:N}:{command.IdempotencyKey}", null, movementId);
        inventoryRepository.Add(movement);
        if (countLine is not null && count is not null) { countLine.Resolve(adjustment.Id); if (count.Lines.Where(line => line.VarianceQuantity != 0).All(line => line.IsResolved)) count.CloseAfterAdjustments(now); }
        InventoryAudit.Adjustment(inventoryRepository, tenant, farm, user, adjustment, "Posted", now, adjustment.Reason, "Posted one signed immutable adjustment movement at current moving-average value.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return InventoryMapper.Adjustment(adjustment);
    }
}
