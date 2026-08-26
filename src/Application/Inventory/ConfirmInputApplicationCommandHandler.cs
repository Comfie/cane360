namespace Cane360.Application.Inventory;

public sealed class ConfirmInputApplicationCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository, IUser user, TimeProvider timeProvider) : IRequestHandler<ConfirmInputApplicationCommand>
{
    public async Task Handle(ConfirmInputApplicationCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireFarmManager(tenant, userId);
        var candidate = await inventoryRepository.GetInputApplicationAsync(tenant.Id, farm.Id, command.InputApplicationId, false, cancellationToken) ?? throw new NotFoundException(command.InputApplicationId.ToString(), "Input application");
        var context = InventoryAccess.RequireOperationalActivity(farm, candidate.ActivityId); var now = timeProvider.GetUtcNow();
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken); await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidate.ActivityId, cancellationToken);
        await inventoryRepository.LockStockIssueLinesAsync(candidate.Lines.Select(x => x.StockIssueLineId).Distinct().Order().ToArray(), cancellationToken);
        var application = await inventoryRepository.GetInputApplicationAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Input application");
        if (application.IsConfirmationRetry(command.IdempotencyKey)) return;
        if (application.Version != command.ExpectedVersion) throw new ConflictException("This application changed after it was loaded. Refresh and try again.");
        var late = now - context.Activity.ActualAt.GetValueOrDefault(application.AppliedAt) > TimeSpan.FromHours(48);
        foreach (var line in application.Lines)
        {
            var issueLine = await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, line.StockIssueLineId, true, cancellationToken) ?? throw new NotFoundException(line.StockIssueLineId.ToString(), "Stock issue line");
            var received = await inventoryRepository.GetFieldReceivedQuantityAsync(issueLine.Id, cancellationToken);
            var applied = await inventoryRepository.GetConfirmedAppliedQuantityAsync(issueLine.Id, cancellationToken);
            var returned = await inventoryRepository.GetPostedReturnedQuantityAsync(issueLine.Id, cancellationToken); var loss = await inventoryRepository.GetApprovedLossQuantityAsync(issueLine.Id, cancellationToken);
            if (applied + line.AppliedQuantity > received) throw new ConflictException($"Applied quantity for {issueLine.ItemCodeSnapshot} exceeds field-received quantity.");
            if (applied + line.AppliedQuantity + returned + loss > issueLine.Quantity) throw new ConflictException($"Application would resolve more than the posted issue quantity for {issueLine.ItemCodeSnapshot}.");
        }
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => application.Confirm(now, userId, command.LateConfirmationReason, late, command.ExpectedVersion, command.IdempotencyKey));
        InventoryAudit.Application(inventoryRepository, tenant, farm, user, application, "ManagerConfirmed", now,
            command.LateConfirmationReason, "Manager confirmation posted applied-input costs.");
        // Reconciliation reads authoritative rows. Persist the confirmation first so the same serializable
        // transaction sees this application's newly confirmed quantity when resolving its control exception.
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        foreach (var line in application.Lines)
        {
            if (!await inventoryRepository.HasOperationalCostPostingAsync(line.Id, OperationalCostCategory.AppliedInput, cancellationToken))
            {
                var posting = OperationalCostPosting.ForApplication(tenant.Id, farm.Id, context.Field.Id, context.Activity.Id, context.Cycle.Id, line, $"application:{line.Id:N}:confirmed");
                inventoryRepository.Add(posting);
                InventoryAudit.Cost(inventoryRepository, tenant, farm, user, posting, "Posted", now,
                    "Manager-confirmed application created the immutable applied-input cost posting.");
            }
            var issueLine = await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, line.StockIssueLineId, true, cancellationToken) ?? throw new NotFoundException(line.StockIssueLineId.ToString(), "Stock issue line");
            await InventoryAccountability.SynchronizeExceptionAsync(inventoryRepository, tenant, farm, user, context.Activity.Id, issueLine, now, cancellationToken);
        }
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }
}
