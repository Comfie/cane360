namespace Cane360.Application.Inventory;

public sealed class ReverseStockReturnCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<ReverseStockReturnCommand>
{
    public async Task Handle(ReverseStockReturnCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);

        StockReturn candidate =
            await inventoryRepository.GetStockReturnAsync(tenant.Id, farm.Id, command.StockReturnId, false,
                cancellationToken)
            ?? throw new NotFoundException(command.StockReturnId.ToString(), "Stock return");
        if (string.IsNullOrWhiteSpace(command.Reason))
        {
            throw InventoryAccess.Failure(nameof(command.Reason), "A reversal reason is required.");
        }

        await using IInventoryTransaction transaction =
            await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidate.ActivityId, cancellationToken);
        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken);
        await inventoryRepository.EnsureStorePostingNotFrozenAsync(tenant.Id, farm.Id, candidate.StoreId,
            cancellationToken);
        await inventoryRepository.LockStockPositionsAsync(
            candidate.Lines.Select(x => x.StockPositionId).Distinct().Order().ToArray(), cancellationToken);
        StockReturn stockReturn =
            await inventoryRepository.GetStockReturnAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken)
            ?? throw new NotFoundException(candidate.Id.ToString(), "Stock return");
        if (stockReturn.IsReversalRetry(command.IdempotencyKey))
        {
            return;
        }

        if (stockReturn.Version != command.ExpectedVersion)
        {
            throw new ConflictException("This stock return changed after it was loaded. Refresh and try again.");
        }

        IReadOnlyList<StockMovement> originals =
            await inventoryRepository.GetReturnMovementsAsync(stockReturn.Id, cancellationToken);
        if (originals.Count != stockReturn.Lines.Count)
        {
            throw new ConflictException("Return history is incomplete and cannot be reversed.");
        }

        foreach (StockMovement original in originals)
        {
            StockLedgerSnapshot snapshot =
                await inventoryRepository.GetPositionSnapshotAsync(original.StockPositionId, cancellationToken);
            if (snapshot.Quantity + original.SignedQuantity < 0 || snapshot.ValueUsd + original.SignedValueUsd < 0)
            {
                throw new ConflictException("Return reversal would make store stock or value negative.");
            }

            StockReturnLine line = stockReturn.Lines.Single(x => x.Id == original.StockReturnLineId);
            inventoryRepository.Add(StockMovement.CreateReturnReversal(original, stockReturn, line,
                timeProvider.GetUtcNow(), userId, $"return:{line.Id:N}:reversed"));
        }

        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion),
            () => stockReturn.MarkReversed(timeProvider.GetUtcNow(), command.IdempotencyKey, command.ExpectedVersion));
        InventoryAudit.Return(inventoryRepository, tenant, farm, user, stockReturn, "Reversed",
            timeProvider.GetUtcNow(),
            command.Reason, "Grower reversed a posted return through exact opposite stock movements.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        foreach (StockReturnLine line in stockReturn.Lines)
        {
            StockIssueLine issueLine =
                await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, line.StockIssueLineId, true,
                    cancellationToken) ??
                throw new NotFoundException(line.StockIssueLineId.ToString(), "Stock issue line");
            await InventoryAccountability.SynchronizeExceptionAsync(inventoryRepository, tenant, farm, user,
                stockReturn.ActivityId, issueLine, timeProvider.GetUtcNow(), cancellationToken);
        }

        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
