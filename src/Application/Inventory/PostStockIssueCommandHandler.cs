using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class PostStockIssueCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<PostStockIssueCommand>
{
    public async Task Handle(PostStockIssueCommand command, CancellationToken cancellationToken)
    {
        try
        {
            await PostOnceAsync(command, cancellationToken);
        }
        catch (InventorySerializationFailureException)
        {
            inventoryRepository.ResetTrackedChanges();
            throw new ConflictException("Another issue consumed the available stock. Refresh availability and try again.");
        }
    }

    private async Task PostOnceAsync(PostStockIssueCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var candidate = await inventoryRepository.GetStockIssueAsync(
            tenant.Id, farm.Id, command.StockIssueId, false, cancellationToken)
            ?? throw new NotFoundException(command.StockIssueId.ToString(), "Stock issue");
        var candidateRequest = await inventoryRepository.GetInputRequestAsync(
            tenant.Id, farm.Id, candidate.InputRequestId, false, cancellationToken)
            ?? throw new NotFoundException(candidate.InputRequestId.ToString(), "Input request");
        var candidateContext = InventoryAccess.RequireOperationalActivity(farm, candidateRequest.ActivityId);

        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidateContext.Activity.Id, cancellationToken);
        await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, farm.Store.Id, cancellationToken);
        await inventoryRepository.LockStockIssueAsync(tenant.Id, farm.Id, candidate.Id, cancellationToken);
        await inventoryRepository.LockInputRequestLinesAsync(
            candidate.Lines.Select(line => line.InputRequestLineId).Distinct().Order().ToArray(), cancellationToken);
        await inventoryRepository.LockStockPositionsAsync(
            candidate.Lines.Select(line => line.StockPositionId).Distinct().Order().ToArray(), cancellationToken);

        var issue = await inventoryRepository.GetStockIssueAsync(
            tenant.Id, farm.Id, candidate.Id, true, cancellationToken)
            ?? throw new NotFoundException(command.StockIssueId.ToString(), "Stock issue");
        if (issue.IsPostingRetry(command.IdempotencyKey)) return;
        var request = await inventoryRepository.GetInputRequestAsync(
            tenant.Id, farm.Id, issue.InputRequestId, true, cancellationToken)
            ?? throw new NotFoundException(issue.InputRequestId.ToString(), "Input request");
        var context = InventoryAccess.RequireOperationalActivity(farm, request.ActivityId);
        var issuer = InventoryAccess.RequireActivePerson(farm, issue.IssuerPersonId, "Issuer");
        if (!issuer.HasEffectiveRole(Cane360.Domain.Activities.PersonRole.Storekeeper, issue.IssueDate))
            throw new ConflictException("The named issuer no longer has an effective Storekeeper role for the issue date.");
        InventoryAccess.RequireActivePerson(farm, issue.RecipientPersonId, "Field recipient");

        foreach (var line in issue.Lines.OrderBy(line => line.Id))
        {
            var approved = request.Lines.Single(requestLine => requestLine.Id == line.InputRequestLineId);
            var issued = await inventoryRepository.GetPostedIssueQuantityAsync(approved.Id, cancellationToken);
            if (line.Quantity > approved.RequestedQuantity - issued)
                throw new ConflictException($"Issue quantity for {line.ItemCodeSnapshot} exceeds the approved outstanding quantity.");
            var stock = await inventoryRepository.GetPositionSnapshotAsync(line.StockPositionId, cancellationToken);
            if (line.Quantity > stock.Quantity)
                throw new ConflictException($"Insufficient available stock for {line.ItemCodeSnapshot}.");
            if (stock.Quantity <= 0 || stock.ValueUsd < 0)
                throw new ConflictException($"No usable moving-average cost exists for {line.ItemCodeSnapshot}.");
            InventoryAccess.ApplyDomainAction(nameof(command.StockIssueId), () =>
                line.LockCost(stock.ValueUsd / stock.Quantity));
        }

        var now = timeProvider.GetUtcNow();
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () =>
            issue.MarkPosted(now, userId, command.IdempotencyKey, command.ExpectedVersion));
        foreach (var line in issue.Lines)
            inventoryRepository.Add(StockMovement.CreateIssue(issue, line, now, userId,
                $"issue:{line.Id:N}:posted"));
        decimal totalIssued = 0;
        foreach (var line in request.Lines)
            totalIssued += await inventoryRepository.GetPostedIssueQuantityAsync(line.Id, cancellationToken) +
                issue.Lines.Where(issueLine => issueLine.InputRequestLineId == line.Id).Sum(issueLine => issueLine.Quantity);
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => request.RecordIssued(totalIssued, request.Version));
        InventoryAudit.Issue(inventoryRepository, tenant, farm, user, issue, "Posted", now,
            issue.LateEntryReason, "Stock issue posted; issue is not crop-cycle consumption and no cost posting was created.");
        foreach (var line in issue.Lines)
        {
            if (await inventoryRepository.GetOpenControlExceptionAsync(tenant.Id, farm.Id, line.Id, cancellationToken) is null)
                inventoryRepository.Add(ControlException.Open(tenant.Id, farm.Id, context.Activity.Id,
                    line.Id, line.Quantity, 0, 0, 0, line.Quantity, now));
        }
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
