namespace Cane360.Application.Inventory;

public sealed class PostStockReceiptCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<PostStockReceiptCommand, StockReceiptDto>
{
    public async Task<StockReceiptDto> Handle(
        PostStockReceiptCommand request, CancellationToken cancellationToken)
    {
        const int maximumAttempts = 3;
        for (int attempt = 1; attempt <= maximumAttempts; attempt++)
        {
            try
            {
                return await PostOnceAsync(request, cancellationToken);
            }
            catch (InventorySerializationFailureException) when (attempt < maximumAttempts)
            {
                inventoryRepository.ResetTrackedChanges();
            }
            catch (InventorySerializationFailureException)
            {
                throw new ConflictException(
                    "Concurrent stock posting did not settle after three attempts. Retry the command.");
            }
        }

        throw new InvalidOperationException("The inventory posting retry loop ended unexpectedly.");
    }

    private async Task<StockReceiptDto> PostOnceAsync(
        PostStockReceiptCommand request, CancellationToken cancellationToken)
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
        if (receipt.IsPostingRetry(request.IdempotencyKey))
        {
            return InventoryMapper.Receipt(tenant, farm, receipt);
        }

        if (receipt.ReceiptType == StockReceiptType.OpeningBalance)
        {
            if (await inventoryRepository.GetOpeningApprovalAsync(
                    receipt.Id, receipt.Version - 1, cancellationToken) is null)
            {
                throw InventoryAccess.Failure(nameof(request.ReceiptId),
                    "The exact opening-balance version is not approved.");
            }
        }

        List<StockPosition> positions = new();
        foreach (StockReceiptLine line in receipt.Lines)
        {
            positions.Add(await inventoryRepository.GetPositionAsync(
                              tenant.Id, farm.Id, farm.Store.Id, line.InventoryItemId,
                              line.InventoryLotId, false, cancellationToken)
                          ?? throw new NotFoundException(line.InventoryItemId.ToString(), "Stock position"));
        }

        await inventoryRepository.LockStockPositionsAsync(
            positions.Select(position => position.Id).Distinct().Order().ToArray(), cancellationToken);

        DateTimeOffset now = timeProvider.GetUtcNow();
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () =>
            receipt.MarkPosted(now, userId, request.IdempotencyKey, request.ExpectedVersion));
        foreach ((StockReceiptLine First, StockPosition Second) pair in receipt.Lines.Zip(positions))
        {
            inventoryRepository.Add(StockMovement.CreateReceipt(
                tenant.Id, farm.Id, farm.Store.Id, pair.Second.Id, pair.First,
                receipt.ReceiptType, receipt.ReceiptDate, now, userId, receipt.ReceivedByPersonId,
                $"receipt:{pair.First.Id:N}:posted"));
        }

        InventoryAudit.Receipt(inventoryRepository, tenant, farm, user, receipt, "Posted", now,
            receipt.LateEntryReason, $"Posted {receipt.ReceiptType} receipt with {receipt.Lines.Count} line(s).");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return InventoryMapper.Receipt(tenant, farm, receipt);
    }
}
