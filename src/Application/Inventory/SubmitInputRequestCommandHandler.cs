using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class SubmitInputRequestCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<SubmitInputRequestCommand>
{
    public async Task Handle(SubmitInputRequestCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        InventoryAccess.RequireGrowerOrManager(tenant, InventoryAccess.RequireUserId(user));
        var inputRequest = await inventoryRepository.GetInputRequestAsync(tenant.Id, farm.Id,
            request.InputRequestId, true, cancellationToken)
            ?? throw new NotFoundException(request.InputRequestId.ToString(), "Input request");
        if (inputRequest.IsSubmissionRetry(request.IdempotencyKey)) return;
        var activity = InventoryAccess.RequireOperationalActivity(farm, inputRequest.ActivityId);
        foreach (var line in inputRequest.Lines)
        {
            var effective = await inventoryRepository.GetEffectiveRuleAsync(tenant.Id, farm.Id,
                line.InventoryItemId, activity.Activity.ActivityTypeId, inputRequest.OperationalDate, cancellationToken);
            if (effective is null || effective.Id != line.InventoryApplicationRuleId || effective.Version != line.RuleVersionSnapshot)
                throw InventoryAccess.Failure(nameof(request.InputRequestId),
                    $"The effective application rule for {line.ItemCodeSnapshot} changed or is missing. Return the request to draft and refresh it.");
            var stock = await inventoryRepository.GetItemStockSnapshotAsync(
                tenant.Id, farm.Id, line.InventoryItemId, cancellationToken);
            decimal? average = stock.Quantity > 0 && stock.ValueUsd >= 0
                ? decimal.Round(stock.ValueUsd / stock.Quantity, 6, MidpointRounding.AwayFromZero)
                : null;
            InventoryAccess.ApplyDomainAction(nameof(request.InputRequestId), () =>
                inputRequest.RefreshSubmissionSnapshot(line.Id, stock.Quantity, average, inputRequest.Version));
        }
        var now = timeProvider.GetUtcNow();
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () =>
            inputRequest.Submit(now, request.IdempotencyKey, request.ExpectedVersion));
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () =>
            inputRequest.OpenApproval(inputRequest.Version));
        InventoryAudit.Request(inventoryRepository, tenant, farm, user, inputRequest, "Submitted",
            now, null, "Input request submitted and opened for role-bound approval.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
