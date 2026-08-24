using Cane360.Domain.Farms;

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
            throw new InvalidOperationException("The rule end date cannot precede its start date.");
        if (ratePerCoverageUnit <= 0) throw new InvalidOperationException("Application rate must be positive.");
        if (lowerTolerancePercent < 0 || upperTolerancePercent < 0)
            throw new InvalidOperationException("Application tolerances cannot be negative.");

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
    public DateOnly EffectiveFrom { get; private set; }
    public DateOnly? EffectiveTo { get; private set; }
    public ApplicationCoverageBasis CoverageBasis { get; private set; }
    public decimal RatePerCoverageUnit { get; private set; }
    public decimal LowerTolerancePercent { get; private set; }
    public decimal UpperTolerancePercent { get; private set; }
    public long Version { get; private set; }

    public static InventoryApplicationRule Create(
        Guid tenantId, Guid farmId, InventoryItem item, Guid activityTypeId,
        DateOnly effectiveFrom, DateOnly? effectiveTo, ApplicationCoverageBasis coverageBasis,
        decimal ratePerCoverageUnit, decimal lowerTolerancePercent, decimal upperTolerancePercent) =>
        new(tenantId, farmId, item, activityTypeId, effectiveFrom, effectiveTo, coverageBasis,
            ratePerCoverageUnit, lowerTolerancePercent, upperTolerancePercent);

    public bool IsEffective(DateOnly date) => EffectiveFrom <= date && (EffectiveTo is null || EffectiveTo >= date);
    public decimal PlannedQuantity(decimal coverage) => Round(coverage * RatePerCoverageUnit);
    public decimal MinimumQuantity(decimal plannedQuantity) => Round(plannedQuantity * (1 - LowerTolerancePercent / 100m));
    public decimal MaximumQuantity(decimal plannedQuantity) => Round(plannedQuantity * (1 + UpperTolerancePercent / 100m));
    public InputApprovalRequirement ApprovalFor(decimal requestedQuantity, decimal plannedQuantity) =>
        requestedQuantity >= MinimumQuantity(plannedQuantity) && requestedQuantity <= MaximumQuantity(plannedQuantity)
            ? InputApprovalRequirement.FarmManagerOrGrower
            : InputApprovalRequirement.GrowerOnly;

    private static decimal Round(decimal value) => decimal.Round(value, 6, MidpointRounding.AwayFromZero);
}
