using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class EditInputRequestLineCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<EditInputRequestLineCommand>
{
    public async Task Handle(EditInputRequestLineCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        InventoryAccess.RequireGrowerOrManager(tenant, InventoryAccess.RequireUserId(user));
        var inputRequest = await inventoryRepository.GetInputRequestAsync(tenant.Id, farm.Id,
            request.InputRequestId, true, cancellationToken)
            ?? throw new NotFoundException(request.InputRequestId.ToString(), "Input request");
        InventoryAccess.RequireOperationalActivity(farm, inputRequest.ActivityId);
        var line = inputRequest.Lines.SingleOrDefault(candidate => candidate.Id == request.InputRequestLineId)
            ?? throw new NotFoundException(request.InputRequestLineId.ToString(), "Input request line");
        var issued = await inventoryRepository.GetPostedIssueQuantityAsync(line.Id, cancellationToken);
        var rule = await inventoryRepository.GetEffectiveRuleAsync(tenant.Id, farm.Id, line.InventoryItemId,
            InventoryAccess.RequireOperationalActivity(farm, inputRequest.ActivityId).Activity.ActivityTypeId,
            inputRequest.OperationalDate, cancellationToken)
            ?? throw InventoryAccess.Failure(nameof(request.InputRequestLineId), "The effective rule is missing.");
        if (rule.Id != line.InventoryApplicationRuleId || rule.Version != line.RuleVersionSnapshot)
            throw InventoryAccess.Failure(nameof(request.InputRequestLineId),
                "The effective application rule changed. Create a fresh request so the new rule snapshot is explicit.");
        InventoryAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () =>
            inputRequest.ChangeLineQuantity(line.Id, request.RequestedQuantity, rule, issued, request.ExpectedVersion));
        InventoryAudit.Request(inventoryRepository, tenant, farm, user, inputRequest, "MaterialEdit",
            timeProvider.GetUtcNow(), null, "Requested quantity changed; prior approval no longer binds the current version.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
