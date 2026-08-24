using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class ReverseStockReceiptCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<ReverseStockReceiptCommand, StockReceiptDto>
{
    public async Task<StockReceiptDto> Handle(
        ReverseStockReceiptCommand request, CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (var attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                return await ReverseOnceAsync(request, cancellationToken);
            }
            catch (InventorySerializationFailureException) when (attempt < maximumAttempts)
            {
                inventoryRepository.ResetTrackedChanges();
            }
            catch (InventorySerializationFailureException)
            {
                throw new ConflictException(
                    "Concurrent stock reversal did not settle after three attempts. Retry the command.");
            }
        }

        throw new InvalidOperationException("The inventory reversal retry loop ended unexpectedly.");
    }

    private async Task<StockReceiptDto> ReverseOnceAsync(
        ReverseStockReceiptCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrower(tenant, userId);
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);

        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, farm.Store.Id, cancellationToken);
        await inventoryRepository.LockReceiptSourceAsync(tenant.Id, farm.Id, request.ReceiptId, cancellationToken);
        var receipt = await inventoryRepository.GetReceiptAsync(
            tenant.Id, farm.Id, request.ReceiptId, true, cancellationToken)
            ?? throw new NotFoundException(request.ReceiptId.ToString(), "Stock receipt");
        if (receipt.IsReversalRetry(request.IdempotencyKey)) return InventoryMapper.Receipt(tenant, farm, receipt);
        var originals = await inventoryRepository.GetReceiptMovementsAsync(receipt.Id, cancellationToken);
        if (originals.Count != receipt.Lines.Count || originals.Any(movement => movement.ReversalOfStockMovementId.HasValue))
        {
            throw new ConflictException("The original posted movement set is incomplete or already corrected.");
        }
        if (await inventoryRepository.HasLaterPositionMovementsAsync(originals, cancellationToken))
        {
            throw new ConflictException(
                "This receipt has dependent later movements. Use an authorised forward correction chain instead of reversal.");
        }
        await inventoryRepository.LockStockPositionsAsync(
            originals.Select(movement => movement.StockPositionId).Distinct().Order().ToArray(), cancellationToken);
        foreach (var group in originals.GroupBy(movement => movement.StockPositionId))
        {
            var current = await inventoryRepository.GetPositionSnapshotAsync(group.Key, cancellationToken);
            var nextQuantity = current.Quantity - group.Sum(movement => movement.SignedQuantity);
            var nextValue = current.ValueUsd - group.Sum(movement => movement.SignedValueUsd);
            if (nextQuantity < 0 || nextValue < 0 || nextQuantity == 0 && nextValue != 0)
            {
                throw new ConflictException(
                    "Reversal would create negative or inconsistent stock quantity/value. Use an authorised forward correction chain.");
            }
        }

        var now = timeProvider.GetUtcNow();
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () =>
            receipt.MarkReversed(now, userId, request.IdempotencyKey, request.ExpectedVersion));
        var lines = receipt.Lines.ToDictionary(line => line.Id);
        foreach (var original in originals)
        {
            var reversal = StockMovement.CreateReversal(
                original, lines[original.StockReceiptLineId!.Value], InventoryAccess.HarareDate(now), now, userId,
                $"movement:{original.Id:N}:reversal");
            inventoryRepository.Add(reversal);
            inventoryRepository.Add(CorrectionRecord.CreateReceiptReversal(
                tenant.Id, farm.Id, receipt.Id, original.Id, reversal.Id, request.Reason, userId, now));
        }
        InventoryAudit.Receipt(inventoryRepository, tenant, farm, user, receipt, "Reversed", now,
            request.Reason, "Posted receipt reversed through linked immutable movements.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return InventoryMapper.Receipt(tenant, farm, receipt);
    }
}
