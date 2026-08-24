namespace Cane360.Domain.Inventory;

public sealed class InputRequestLine : BaseEntity
{
    private InputRequestLine() { }

    private InputRequestLine(
        Guid tenantId, Guid farmId, Guid requestId, int lineNumber, InventoryItem item,
        InventoryApplicationRule rule, decimal plannedCoverage, decimal requestedQuantity,
        decimal availableQuantitySnapshot, decimal? estimatedUnitCostUsd)
    {
        if (plannedCoverage <= 0 || requestedQuantity <= 0)
            throw new InvalidOperationException("Planned coverage and requested quantity must be positive.");
        TenantId = tenantId;
        FarmId = farmId;
        InputRequestId = requestId;
        LineNumber = lineNumber;
        InventoryItemId = item.Id;
        UnitOfMeasureId = item.StockUnitId;
        ItemCodeSnapshot = item.Code;
        ItemNameSnapshot = item.Name;
        UnitCodeSnapshot = item.StockUnitCode;
        InventoryApplicationRuleId = rule.Id;
        RuleVersionSnapshot = rule.Version;
        RuleEffectiveFromSnapshot = rule.EffectiveFrom;
        RuleEffectiveToSnapshot = rule.EffectiveTo;
        CoverageBasisSnapshot = rule.CoverageBasis;
        PlannedCoverage = Round(plannedCoverage);
        PlannedRate = rule.RatePerCoverageUnit;
        PlannedQuantity = rule.PlannedQuantity(plannedCoverage);
        RequestedQuantity = Round(requestedQuantity);
        LowerTolerancePercent = rule.LowerTolerancePercent;
        UpperTolerancePercent = rule.UpperTolerancePercent;
        ApprovalRequirement = rule.ApprovalFor(RequestedQuantity, PlannedQuantity);
        AvailableQuantitySnapshot = Round(availableQuantitySnapshot);
        EstimatedUnitCostUsdSnapshot = estimatedUnitCostUsd.HasValue ? Round(estimatedUnitCostUsd.Value) : null;
        EstimatedValueUsdSnapshot = estimatedUnitCostUsd.HasValue ? Round(RequestedQuantity * estimatedUnitCostUsd.Value) : null;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid InputRequestId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty;
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public Guid InventoryApplicationRuleId { get; private set; }
    public long RuleVersionSnapshot { get; private set; }
    public DateOnly RuleEffectiveFromSnapshot { get; private set; }
    public DateOnly? RuleEffectiveToSnapshot { get; private set; }
    public ApplicationCoverageBasis CoverageBasisSnapshot { get; private set; }
    public decimal PlannedCoverage { get; private set; }
    public decimal PlannedRate { get; private set; }
    public decimal PlannedQuantity { get; private set; }
    public decimal RequestedQuantity { get; private set; }
    public decimal LowerTolerancePercent { get; private set; }
    public decimal UpperTolerancePercent { get; private set; }
    public InputApprovalRequirement ApprovalRequirement { get; private set; }
    public decimal AvailableQuantitySnapshot { get; private set; }
    public decimal? EstimatedUnitCostUsdSnapshot { get; private set; }
    public decimal? EstimatedValueUsdSnapshot { get; private set; }

    internal static InputRequestLine Create(
        Guid tenantId, Guid farmId, Guid requestId, int lineNumber, InventoryItem item,
        InventoryApplicationRule rule, decimal plannedCoverage, decimal requestedQuantity,
        decimal availableQuantitySnapshot, decimal? estimatedUnitCostUsd) =>
        new(tenantId, farmId, requestId, lineNumber, item, rule, plannedCoverage, requestedQuantity,
            availableQuantitySnapshot, estimatedUnitCostUsd);

    internal void ChangeRequestedQuantity(decimal requestedQuantity, InventoryApplicationRule rule)
    {
        if (requestedQuantity <= 0) throw new InvalidOperationException("Requested quantity must be positive.");
        RequestedQuantity = Round(requestedQuantity);
        ApprovalRequirement = rule.ApprovalFor(RequestedQuantity, PlannedQuantity);
        EstimatedValueUsdSnapshot = EstimatedUnitCostUsdSnapshot.HasValue
            ? Round(RequestedQuantity * EstimatedUnitCostUsdSnapshot.Value)
            : null;
    }

    internal void RefreshSubmissionSnapshots(decimal availableQuantity, decimal? estimatedUnitCostUsd)
    {
        AvailableQuantitySnapshot = Round(availableQuantity);
        EstimatedUnitCostUsdSnapshot = estimatedUnitCostUsd.HasValue ? Round(estimatedUnitCostUsd.Value) : null;
        EstimatedValueUsdSnapshot = estimatedUnitCostUsd.HasValue
            ? Round(RequestedQuantity * estimatedUnitCostUsd.Value)
            : null;
    }

    private static decimal Round(decimal value) => decimal.Round(value, 6, MidpointRounding.AwayFromZero);
}
