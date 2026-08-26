namespace Cane360.Application.Inventory;

public sealed class PostStockReturnCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<PostStockReturnCommand>
{
    public async Task Handle(PostStockReturnCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var candidate = await inventoryRepository.GetStockReturnAsync(tenant.Id, farm.Id, command.StockReturnId, false, cancellationToken) ?? throw new NotFoundException(command.StockReturnId.ToString(), "Stock return");
        var context = InventoryAccess.RequireOperationalActivity(farm, candidate.ActivityId); var now = timeProvider.GetUtcNow();
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken); await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidate.ActivityId, cancellationToken); await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken); await inventoryRepository.EnsureStorePostingNotFrozenAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken); await inventoryRepository.LockStockIssueLinesAsync(candidate.Lines.Select(x => x.StockIssueLineId).Distinct().Order().ToArray(), cancellationToken); await inventoryRepository.LockStockPositionsAsync(candidate.Lines.Select(x => x.StockPositionId).Distinct().Order().ToArray(), cancellationToken);
        var stockReturn = await inventoryRepository.GetStockReturnAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Stock return");
        if (stockReturn.IsPostingRetry(command.IdempotencyKey)) return; if (stockReturn.Version != command.ExpectedVersion) throw new ConflictException("This stock return changed after it was loaded. Refresh and try again.");
        foreach (var line in stockReturn.Lines)
        {
            var issueLine = await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, line.StockIssueLineId, true, cancellationToken) ?? throw new NotFoundException(line.StockIssueLineId.ToString(), "Stock issue line");
            var values = await InventoryAccountability.GetAsync(inventoryRepository, issueLine, cancellationToken);
            if (values.Applied + values.Returned + line.Quantity + values.Loss > issueLine.Quantity) throw new ConflictException($"Return for {issueLine.ItemCodeSnapshot} exceeds unresolved issued quantity.");
        }
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => stockReturn.MarkPosted(now, userId, command.IdempotencyKey, command.ExpectedVersion));
        InventoryAudit.Return(inventoryRepository, tenant, farm, user, stockReturn, "Posted", now, null,
            "Store received the return; stock was restored at the locked issue cost.");
        foreach (var line in stockReturn.Lines)
        {
            inventoryRepository.Add(StockMovement.CreateReturn(stockReturn, line, now, userId, $"return:{line.Id:N}:posted"));
            var issueLine = await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, line.StockIssueLineId, true, cancellationToken) ?? throw new NotFoundException(line.StockIssueLineId.ToString(), "Stock issue line");
            await InventoryAccountability.SynchronizeExceptionAsync(inventoryRepository, tenant, farm, user, context.Activity.Id, issueLine, now, cancellationToken);
        }
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }
}
