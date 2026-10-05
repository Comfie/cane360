namespace Cane360.Application.Inventory;

public sealed class CreateInputApplicationCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider)
    : IRequestHandler<CreateInputApplicationCommand, Guid>
{
    public async Task<Guid> Handle(CreateInputApplicationCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);
        (Field Field, CropCycle Cycle, Activity Activity) context =
            InventoryAccess.RequireOperationalActivity(farm, command.ActivityId);
        DateTimeOffset now = timeProvider.GetUtcNow();
        await using IInventoryTransaction transaction =
            await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, command.ActivityId, cancellationToken);
        await inventoryRepository.LockFieldReceiptLinesAsync(
            command.Lines.Select(x => x.FieldReceiptLineId).Distinct().Order().ToArray(), cancellationToken);
        await inventoryRepository.LockStockIssueLinesAsync(
            command.Lines.Select(x => x.StockIssueLineId).Distinct().Order().ToArray(), cancellationToken);
        InputApplication application = InventoryAccess.ApplyDomainAction(nameof(command.VerifiedCoverage),
            () => InputApplication.Create(tenant.Id, farm.Id, command.ActivityId, command.AppliedAt.ToUniversalTime(),
                command.CoverageBasis, command.VerifiedCoverage, now, userId));
        foreach (CreateInputApplicationLineCommand lineCommand in command.Lines)
        {
            FieldReceipt receipt =
                (await inventoryRepository.GetFieldReceiptsAsync(tenant.Id, farm.Id, null, true, cancellationToken))
                .SingleOrDefault(x => x.ActivityId == command.ActivityId && x.Status == FieldReceiptStatus.Recorded &&
                                      x.Lines.Any(line => line.Id == lineCommand.FieldReceiptLineId))
                ?? throw new NotFoundException(lineCommand.FieldReceiptLineId.ToString(),
                    "Recorded field receipt line for this activity");
            FieldReceiptLine receiptLine = receipt.Lines.Single(line => line.Id == lineCommand.FieldReceiptLineId);
            StockIssueLine issueLine =
                await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, lineCommand.StockIssueLineId, true,
                    cancellationToken) ??
                throw new NotFoundException(lineCommand.StockIssueLineId.ToString(), "Stock issue line");
            if (receiptLine.StockIssueLineId != issueLine.Id ||
                receiptLine.InventoryItemId != issueLine.InventoryItemId ||
                receiptLine.InventoryLotId != issueLine.InventoryLotId)
            {
                throw InventoryAccess.Failure(nameof(command.Lines),
                    "Application line must retain the field receipt, item, and lot trace.");
            }

            StockIssue issue =
                await inventoryRepository.GetStockIssueAsync(tenant.Id, farm.Id, receipt.StockIssueId, false,
                    cancellationToken)
                ?? throw new NotFoundException(receipt.StockIssueId.ToString(), "Stock issue");
            InputRequest request =
                await inventoryRepository.GetInputRequestAsync(tenant.Id, farm.Id, issue.InputRequestId, false,
                    cancellationToken)
                ?? throw new NotFoundException(issue.InputRequestId.ToString(), "Input request");
            if (request.ActivityId != command.ActivityId || request.FieldId != context.Field.Id ||
                request.CropCycleId != context.Cycle.Id)
            {
                throw InventoryAccess.Failure(nameof(command.Lines),
                    "The field receipt trace does not belong to this activity, field, and crop cycle.");
            }

            InventoryApplicationRule rule =
                await inventoryRepository.GetEffectiveRuleAsync(tenant.Id, farm.Id, issueLine.InventoryItemId,
                    context.Activity.ActivityTypeId, InventoryAccess.HarareDate(command.AppliedAt),
                    cancellationToken) ?? throw InventoryAccess.Failure(nameof(command.Lines),
                    "No effective application rule exists for this item and activity.");
            if (rule.CoverageBasis != command.CoverageBasis)
            {
                throw InventoryAccess.Failure(nameof(command.CoverageBasis),
                    "Application coverage must use the effective rule basis.");
            }

            InventoryAccess.ApplyDomainAction(nameof(command.Lines),
                () => application.AddLine(receiptLine, issueLine, rule, lineCommand.AppliedQuantity));
        }

        inventoryRepository.Add(application);
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return application.Id;
    }
}
