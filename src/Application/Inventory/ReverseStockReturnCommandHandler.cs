namespace Cane360.Application.Inventory;

public sealed class ReverseStockReturnCommandHandler(IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository, IUser user, TimeProvider timeProvider) : IRequestHandler<ReverseStockReturnCommand>
{
    public async Task Handle(ReverseStockReturnCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrower(tenant, userId);
        var candidate = await inventoryRepository.GetStockReturnAsync(tenant.Id, farm.Id, command.StockReturnId, false, cancellationToken)
            ?? throw new NotFoundException(command.StockReturnId.ToString(), "Stock return");
        if (string.IsNullOrWhiteSpace(command.Reason)) throw InventoryAccess.Failure(nameof(command.Reason), "A reversal reason is required.");
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidate.ActivityId, cancellationToken);
        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken);
        await inventoryRepository.LockStockPositionsAsync(candidate.Lines.Select(x => x.StockPositionId).Distinct().Order().ToArray(), cancellationToken);
        var stockReturn = await inventoryRepository.GetStockReturnAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken)
            ?? throw new NotFoundException(candidate.Id.ToString(), "Stock return");
        if (stockReturn.IsReversalRetry(command.IdempotencyKey)) return;
        if (stockReturn.Version != command.ExpectedVersion) throw new ConflictException("This stock return changed after it was loaded. Refresh and try again.");
        var originals = await inventoryRepository.GetReturnMovementsAsync(stockReturn.Id, cancellationToken);
        if (originals.Count != stockReturn.Lines.Count) throw new ConflictException("Return history is incomplete and cannot be reversed.");
        foreach (var original in originals)
        {
            var snapshot = await inventoryRepository.GetPositionSnapshotAsync(original.StockPositionId, cancellationToken);
            if (snapshot.Quantity + original.SignedQuantity < 0 || snapshot.ValueUsd + original.SignedValueUsd < 0)
                throw new ConflictException("Return reversal would make store stock or value negative.");
            var line = stockReturn.Lines.Single(x => x.Id == original.StockReturnLineId);
            inventoryRepository.Add(StockMovement.CreateReturnReversal(original, stockReturn, line, timeProvider.GetUtcNow(), userId, $"return:{line.Id:N}:reversed"));
        }
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => stockReturn.MarkReversed(timeProvider.GetUtcNow(), command.IdempotencyKey, command.ExpectedVersion));
        InventoryAudit.Return(inventoryRepository, tenant, farm, user, stockReturn, "Reversed", timeProvider.GetUtcNow(),
            command.Reason, "Grower reversed a posted return through exact opposite stock movements.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }
}
