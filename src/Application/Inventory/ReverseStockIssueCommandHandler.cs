namespace Cane360.Application.Inventory;

public sealed class ReverseStockIssueCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<ReverseStockIssueCommand>
{
    public async Task Handle(ReverseStockIssueCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);

        StockIssue candidate = await inventoryRepository.GetStockIssueAsync(
                                   tenant.Id, farm.Id, command.StockIssueId, false, cancellationToken)
                               ?? throw new NotFoundException(command.StockIssueId.ToString(), "Stock issue");
        await using IInventoryTransaction transaction =
            await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, farm.Store.Id, cancellationToken);
        await inventoryRepository.EnsureStorePostingNotFrozenAsync(tenant.Id, farm.Id, farm.Store.Id,
            cancellationToken);
        await inventoryRepository.LockStockIssueAsync(tenant.Id, farm.Id, candidate.Id, cancellationToken);
        await inventoryRepository.LockInputRequestLinesAsync(
            candidate.Lines.Select(line => line.InputRequestLineId).Distinct().Order().ToArray(), cancellationToken);
        await inventoryRepository.LockStockPositionsAsync(
            candidate.Lines.Select(line => line.StockPositionId).Distinct().Order().ToArray(), cancellationToken);
        StockIssue issue = await inventoryRepository.GetStockIssueAsync(
                               tenant.Id, farm.Id, candidate.Id, true, cancellationToken)
                           ?? throw new NotFoundException(candidate.Id.ToString(), "Stock issue");
        if (issue.IsReversalRetry(command.IdempotencyKey))
        {
            return;
        }

        if (await inventoryRepository.HasDependentFieldAccountabilityAsync(issue.Id, cancellationToken))
        {
            throw new ConflictException(
                "This issue has dependent field-accountability records and cannot be reversed.");
        }

        IReadOnlyList<StockMovement> originals =
            await inventoryRepository.GetIssueMovementsAsync(issue.Id, cancellationToken);
        if (originals.Count != issue.Lines.Count)
        {
            throw new ConflictException("The posted issue movement chain is incomplete and cannot be reversed.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        foreach (StockMovement original in originals)
        {
            StockIssueLine line = issue.Lines.Single(item => item.Id == original.StockIssueLineId);
            if (original.SignedQuantity != -line.Quantity || original.SignedValueUsd != -line.IssueValueUsd)
            {
                throw new ConflictException("The issue ledger values do not match their locked source snapshots.");
            }

            StockMovement reversal = StockMovement.CreateIssueReversal(original, issue, line,
                InventoryAccess.HarareDate(now), now, userId, $"issue:{line.Id:N}:reversal");
            inventoryRepository.Add(reversal);
            inventoryRepository.Add(CorrectionRecord.CreateIssueReversal(tenant.Id, farm.Id,
                issue.Id, original.Id, reversal.Id, command.Reason, userId, now));
        }

        InputRequest request = await inventoryRepository.GetInputRequestAsync(
                                   tenant.Id, farm.Id, issue.InputRequestId, true, cancellationToken)
                               ?? throw new NotFoundException(issue.InputRequestId.ToString(), "Input request");
        decimal remainingIssued = 0;
        foreach (InputRequestLine requestLine in request.Lines)
        {
            decimal currentlyIssued =
                await inventoryRepository.GetPostedIssueQuantityAsync(requestLine.Id, cancellationToken);
            remainingIssued += currentlyIssued - issue.Lines
                .Where(line => line.InputRequestLineId == requestLine.Id).Sum(line => line.Quantity);
        }

        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () =>
            request.RecordIssueReversed(remainingIssued, request.Version));
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () =>
            issue.MarkReversed(now, command.IdempotencyKey, command.ExpectedVersion));
        InventoryAudit.Issue(inventoryRepository, tenant, farm, user, issue, "Reversed",
            now, command.Reason, "Grower-authorised issue reversal appended exact opposite movements.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
