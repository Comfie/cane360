namespace Cane360.Application.Inventory;

public sealed class DecideInventoryLossCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository, IUser user, TimeProvider timeProvider) : IRequestHandler<DecideInventoryLossCommand>
{
    public async Task Handle(DecideInventoryLossCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrower(tenant, userId);
        var candidate = await inventoryRepository.GetInventoryLossAsync(tenant.Id, farm.Id, command.InventoryLossId, false, cancellationToken) ?? throw new NotFoundException(command.InventoryLossId.ToString(), "Inventory loss"); var context = InventoryAccess.RequireOperationalActivity(farm, candidate.ActivityId); var now = timeProvider.GetUtcNow();
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken); await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidate.ActivityId, cancellationToken); await inventoryRepository.LockStockIssueLinesAsync([candidate.StockIssueLineId], cancellationToken);
        var loss = await inventoryRepository.GetInventoryLossAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Inventory loss");
        if (loss.Version != command.ExpectedVersion) throw new ConflictException("This loss changed after it was loaded. Refresh and try again.");
        var existing = await inventoryRepository.GetInventoryLossApprovalAsync(loss.Id, command.ExpectedVersion, cancellationToken); if (existing?.IdempotencyKey == command.IdempotencyKey) return;
        if (existing is not null) throw new ConflictException("This loss version has already been decided.");
        if (command.Outcome == ApprovalOutcome.Approved)
        {
            var issueLine = await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, loss.StockIssueLineId, true, cancellationToken) ?? throw new NotFoundException(loss.StockIssueLineId.ToString(), "Stock issue line"); var values = await InventoryAccountability.GetAsync(inventoryRepository, issueLine, cancellationToken);
            if (values.Applied + values.Returned + values.Loss + loss.Quantity > issueLine.Quantity) throw new ConflictException("Approved loss would resolve more than the posted issue quantity.");
        }
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => loss.Decide(command.Outcome, now, command.ExpectedVersion)); inventoryRepository.Add(ApprovalDecision.CreateInventoryLossDecision(tenant.Id, farm.Id, loss.Id, command.ExpectedVersion, command.Outcome, userId, TenantSecurityRoles.Grower, now, command.Reason, command.IdempotencyKey));
        InventoryAudit.Loss(inventoryRepository, tenant, farm, user, loss, command.Outcome.ToString(), now,
            command.Reason, "Grower made the inventory-loss decision against the exact loss version.");
        if (command.Outcome == ApprovalOutcome.Approved)
        {
            var posting = OperationalCostPosting.ForLoss(tenant.Id, farm.Id, context.Field.Id, context.Activity.Id, context.Cycle.Id, loss, $"loss:{loss.Id:N}:approved");
            inventoryRepository.Add(posting);
            InventoryAudit.Cost(inventoryRepository, tenant, farm, user, posting, "Posted", now,
                "Grower-approved field loss created the immutable loss cost posting.");
            var issueLine = await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, loss.StockIssueLineId, true, cancellationToken) ?? throw new NotFoundException(loss.StockIssueLineId.ToString(), "Stock issue line"); await InventoryAccountability.SynchronizeExceptionAsync(inventoryRepository, tenant, farm, user, context.Activity.Id, issueLine, now, cancellationToken);
        }
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }
}
