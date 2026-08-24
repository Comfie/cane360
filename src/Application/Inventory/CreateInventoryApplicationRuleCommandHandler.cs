using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateInventoryApplicationRuleCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider)
    : IRequestHandler<CreateInventoryApplicationRuleCommand, InventoryApplicationRuleDto>
{
    public async Task<InventoryApplicationRuleDto> Handle(
        CreateInventoryApplicationRuleCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        InventoryAccess.RequireGrowerOrManager(tenant, InventoryAccess.RequireUserId(user));
        var item = await inventoryRepository.GetItemAsync(tenant.Id, farm.Id, request.InventoryItemId, false, cancellationToken)
            ?? throw new NotFoundException(request.InventoryItemId.ToString(), "Inventory item");
        if (!tenant.ActivityTypes.Any(type => type.Id == request.ActivityTypeId))
            throw new NotFoundException(request.ActivityTypeId.ToString(), "Activity type");
        if (!Enum.TryParse<ApplicationCoverageBasis>(request.CoverageBasis, true, out var basis))
            throw InventoryAccess.Failure(nameof(request.CoverageBasis), "Select a supported coverage basis.");
        var existing = await inventoryRepository.GetRulesAsync(tenant.Id, farm.Id, cancellationToken);
        if (existing.Any(rule => rule.InventoryItemId == item.Id && rule.ActivityTypeId == request.ActivityTypeId &&
            request.EffectiveFrom <= (rule.EffectiveTo ?? DateOnly.MaxValue) &&
            rule.EffectiveFrom <= (request.EffectiveTo ?? DateOnly.MaxValue)))
            throw new ConflictException("This item and activity type already have a rule overlapping the selected dates.");
        var rule = InventoryAccess.ApplyDomainAction(nameof(request.RatePerCoverageUnit), () =>
            InventoryApplicationRule.Create(tenant.Id, farm.Id, item, request.ActivityTypeId,
                request.EffectiveFrom, request.EffectiveTo, basis, request.RatePerCoverageUnit,
                request.LowerTolerancePercent, request.UpperTolerancePercent));
        inventoryRepository.Add(rule);
        InventoryAudit.Rule(inventoryRepository, tenant, farm, user, rule, "Created",
            timeProvider.GetUtcNow(), "Effective-dated inventory application rule created.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return new InventoryApplicationRuleDto(rule.Id, rule.InventoryItemId, rule.ActivityTypeId,
            rule.EffectiveFrom, rule.EffectiveTo, rule.CoverageBasis.ToString(), rule.RatePerCoverageUnit,
            rule.LowerTolerancePercent, rule.UpperTolerancePercent, rule.UnitCodeSnapshot, rule.Version);
    }
}
