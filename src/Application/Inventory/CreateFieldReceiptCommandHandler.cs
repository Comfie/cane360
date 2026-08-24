namespace Cane360.Application.Inventory;

public sealed class CreateFieldReceiptCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<CreateFieldReceiptCommand, Guid>
{
    public async Task<Guid> Handle(CreateFieldReceiptCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var candidate = await inventoryRepository.GetStockIssueAsync(tenant.Id, farm.Id, command.StockIssueId, false, cancellationToken)
            ?? throw new NotFoundException(command.StockIssueId.ToString(), "Stock issue");
        var request = await inventoryRepository.GetInputRequestAsync(tenant.Id, farm.Id, candidate.InputRequestId, false, cancellationToken)
            ?? throw new NotFoundException(candidate.InputRequestId.ToString(), "Input request");
        var context = InventoryAccess.RequireOperationalActivity(farm, request.ActivityId);
        if (candidate.Status != StockIssueStatus.Posted) throw InventoryAccess.Failure(nameof(command.StockIssueId), "Only a posted issue can be field received.");
        if (command.ActivityId != request.ActivityId || command.FieldId != request.FieldId || command.CropCycleId != request.CropCycleId)
            throw InventoryAccess.Failure(nameof(command.ActivityId), "Field, crop cycle, and activity must match the posted issue request chain.");
        var now = timeProvider.GetUtcNow(); var delay = InventoryAccess.EntryDelay(InventoryAccess.HarareDate(command.ReceivedAt), now);
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, request.ActivityId, cancellationToken);
        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, candidate.StoreId, cancellationToken);
        await inventoryRepository.LockStockIssueAsync(tenant.Id, farm.Id, candidate.Id, cancellationToken);
        await inventoryRepository.LockStockIssueLinesAsync(command.Lines.Select(x => x.StockIssueLineId).Distinct().Order().ToArray(), cancellationToken);
        var issue = await inventoryRepository.GetStockIssueAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Stock issue");
        InventoryAccess.RequireActivePerson(farm, command.RecipientPersonId, "Field recipient");
        var receipt = InventoryAccess.ApplyDomainAction(nameof(command.ReceivedAt), () => FieldReceipt.Create(tenant.Id, farm.Id, issue, command.FieldId, command.CropCycleId, command.ActivityId, command.RecipientPersonId, command.ReceivedAt.ToUniversalTime(), now, userId, command.LateEntryReason, delay));
        foreach (var requestLine in command.Lines)
        {
            var issueLine = issue.Lines.SingleOrDefault(x => x.Id == requestLine.StockIssueLineId) ?? throw new NotFoundException(requestLine.StockIssueLineId.ToString(), "Stock issue line");
            var received = await inventoryRepository.GetFieldReceivedQuantityAsync(issueLine.Id, cancellationToken);
            if (received + requestLine.Quantity > issueLine.Quantity) throw new ConflictException($"Field receipt for {issueLine.ItemCodeSnapshot} exceeds the posted issue quantity.");
            InventoryAccess.ApplyDomainAction(nameof(command.Lines), () => receipt.AddLine(issueLine, requestLine.Quantity));
        }
        inventoryRepository.Add(receipt);
        InventoryAudit.FieldReceipt(inventoryRepository, tenant, farm, user, receipt, "Recorded", now,
            receipt.LateEntryReason, "Field receipt recorded; store stock is unchanged.");
        InventoryAudit.Issue(inventoryRepository, tenant, farm, user, issue, "FieldReceived", now, receipt.LateEntryReason, "Field receipt recorded; store stock is unchanged.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return receipt.Id;
    }
}
