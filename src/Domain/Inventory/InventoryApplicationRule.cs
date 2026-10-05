namespace Cane360.Domain.Inventory;

public sealed class InventoryApplicationRule : BaseAuditableEntity
{
    private InventoryApplicationRule() { }

    private InventoryApplicationRule(
        Guid tenantId, Guid farmId, InventoryItem item, Guid activityTypeId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, ApplicationCoverageBasis coverageBasis,
        decimal ratePerCoverageUnit, decimal lowerTolerancePercent, decimal upperTolerancePercent)
    {
        if (effectiveTo.HasValue && effectiveTo < effectiveFrom)
        {
            throw new InvalidOperationException("The rule end date cannot precede its start date.");
        }

        if (ratePerCoverageUnit <= 0)
        {
            throw new InvalidOperationException("Application rate must be positive.");
        }

        if (lowerTolerancePercent < 0 || upperTolerancePercent < 0)
        {
            throw new InvalidOperationException("Application tolerances cannot be negative.");
        }

        TenantId = tenantId;
        FarmId = farmId;
        InventoryItemId = item.Id;
        ActivityTypeId = activityTypeId;
        UnitOfMeasureId = item.StockUnitId;
        UnitCodeSnapshot = item.StockUnitCode;
        EffectiveFrom = effectiveFrom;
        EffectiveTo = effectiveTo;
        CoverageBasis = coverageBasis;
        RatePerCoverageUnit = Round(ratePerCoverageUnit);
        LowerTolerancePercent = Round(lowerTolerancePercent);
        UpperTolerancePercent = Round(upperTolerancePercent);
        Version = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid ActivityTypeId { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public DateOnly EffectiveFrom { get; }
    public DateOnly? EffectiveTo { get; private set; }
    public ApplicationCoverageBasis CoverageBasis { get; private set; }
    public decimal RatePerCoverageUnit { get; }
    public decimal LowerTolerancePercent { get; }
    public decimal UpperTolerancePercent { get; }
    public long Version { get; private set; }

    public static InventoryApplicationRule Create(
        Guid tenantId, Guid farmId, InventoryItem item, Guid activityTypeId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, ApplicationCoverageBasis coverageBasis,
        decimal ratePerCoverageUnit, decimal lowerTolerancePercent, decimal upperTolerancePercent)
    {
        return new InventoryApplicationRule(tenantId, farmId, item, activityTypeId, effectiveFrom, effectiveTo,
            coverageBasis,
            ratePerCoverageUnit, lowerTolerancePercent, upperTolerancePercent);
    }

    public bool IsEffective(DateOnly date)
    {
        return EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);
    }

    public void End(DateOnly effectiveTo, long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This application rule changed after it was loaded.");
        }

        if (effectiveTo < EffectiveFrom || (EffectiveTo.HasValue && effectiveTo > EffectiveTo))
        {
            throw new InvalidOperationException("The end date must stay within the rule's effective range.");
        }

        EffectiveTo = effectiveTo;
        Version++;
    }

    public decimal PlannedQuantity(decimal coverage)
    {
        return Round(coverage * RatePerCoverageUnit);
    }

    public decimal MinimumQuantity(decimal plannedQuantity)
    {
        return Round(plannedQuantity * (1 - LowerTolerancePercent / 100m));
    }

    public decimal MaximumQuantity(decimal plannedQuantity)
    {
        return Round(plannedQuantity * (1 + UpperTolerancePercent / 100m));
    }

    public InputApprovalRequirement ApprovalFor(decimal requestedQuantity, decimal plannedQuantity)
    {
        return requestedQuantity >= MinimumQuantity(plannedQuantity) &&
               requestedQuantity <= MaximumQuantity(plannedQuantity)
            ? InputApprovalRequirement.FarmManagerOrGrower
            : InputApprovalRequirement.GrowerOnly;
    }

    private static decimal Round(decimal value)
    {
        return decimal.Round(value, 6, MidpointRounding.AwayFromZero);
    }
}
