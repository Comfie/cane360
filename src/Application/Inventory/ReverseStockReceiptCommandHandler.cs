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
        for (int attempt = 1; attempt <= maximumAttempts; attempt++)
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
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);

        await using IInventoryTransaction transaction =
            await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);

        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, farm.Store.Id, cancellationToken);
        await inventoryRepository.EnsureStorePostingNotFrozenAsync(tenant.Id, farm.Id, farm.Store.Id,
            cancellationToken);
        await inventoryRepository.LockReceiptSourceAsync(tenant.Id, farm.Id, request.ReceiptId, cancellationToken);
        StockReceipt receipt = await inventoryRepository.GetReceiptAsync(
                                   tenant.Id, farm.Id, request.ReceiptId, true, cancellationToken)
                               ?? throw new NotFoundException(request.ReceiptId.ToString(), "Stock receipt");
        if (receipt.IsReversalRetry(request.IdempotencyKey))
        {
            return InventoryMapper.Receipt(tenant, farm, receipt);
        }

        IReadOnlyList<StockMovement> originals =
            await inventoryRepository.GetReceiptMovementsAsync(receipt.Id, cancellationToken);
        if (originals.Count != receipt.Lines.Count ||
            originals.Any(movement => movement.ReversalOfStockMovementId.HasValue))
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
        foreach (IGrouping<Guid, StockMovement> group in originals.GroupBy(movement => movement.StockPositionId))
        {
            StockLedgerSnapshot current =
                await inventoryRepository.GetPositionSnapshotAsync(group.Key, cancellationToken);
            decimal nextQuantity = current.Quantity - group.Sum(movement => movement.SignedQuantity);
            decimal nextValue = current.ValueUsd - group.Sum(movement => movement.SignedValueUsd);
            if (nextQuantity < 0 || nextValue < 0 || (nextQuantity == 0 && nextValue != 0))
            {
                throw new ConflictException(
                    "Reversal would create negative or inconsistent stock quantity/value. Use an authorised forward correction chain.");
            }
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () =>
            receipt.MarkReversed(now, userId, request.IdempotencyKey, request.ExpectedVersion));
        Dictionary<Guid, StockReceiptLine> lines = receipt.Lines.ToDictionary(line => line.Id);
        foreach (StockMovement original in originals)
        {
            StockMovement reversal = StockMovement.CreateReversal(
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
