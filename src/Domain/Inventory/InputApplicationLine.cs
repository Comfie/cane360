namespace Cane360.Domain.Inventory;

public sealed class InputApplicationLine : BaseEntity
{
    private InputApplicationLine() { }
    private InputApplicationLine(Guid tenantId, Guid farmId, Guid applicationId, FieldReceiptLine receiptLine,
        StockIssueLine issueLine, InventoryApplicationRule rule, decimal coverage, decimal quantity)
    {
        if (quantity <= 0) throw new InvalidOperationException("Applied quantity must be positive.");
        TenantId = tenantId; FarmId = farmId; InputApplicationId = applicationId; FieldReceiptLineId = receiptLine.Id;
        StockIssueLineId = issueLine.Id; InventoryItemId = issueLine.InventoryItemId; InventoryLotId = issueLine.InventoryLotId;
        UnitOfMeasureId = issueLine.UnitOfMeasureId; ItemCodeSnapshot = issueLine.ItemCodeSnapshot; ItemNameSnapshot = issueLine.ItemNameSnapshot;
        LotCodeSnapshot = issueLine.LotCodeSnapshot; UnitCodeSnapshot = issueLine.UnitCodeSnapshot; IssueUnitCostUsdSnapshot = issueLine.IssueUnitCostUsd!.Value;
        AppliedQuantity = decimal.Round(quantity, 6, MidpointRounding.AwayFromZero); CoverageSnapshot = coverage;
        ActualRate = decimal.Round(quantity / coverage, 6, MidpointRounding.AwayFromZero); RuleIdSnapshot = rule.Id; RuleVersionSnapshot = rule.Version;
        RuleRateSnapshot = rule.RatePerCoverageUnit; LowerTolerancePercentSnapshot = rule.LowerTolerancePercent; UpperTolerancePercentSnapshot = rule.UpperTolerancePercent;
        RateVariance = decimal.Round(ActualRate - rule.RatePerCoverageUnit, 6, MidpointRounding.AwayFromZero);
    }
    public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid InputApplicationId { get; private set; }
    public Guid FieldReceiptLineId { get; private set; } public Guid StockIssueLineId { get; private set; } public Guid InventoryItemId { get; private set; }
    public Guid? InventoryLotId { get; private set; } public Guid UnitOfMeasureId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty; public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string? LotCodeSnapshot { get; private set; } public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public decimal IssueUnitCostUsdSnapshot { get; private set; } public decimal AppliedQuantity { get; private set; } public decimal CoverageSnapshot { get; private set; }
    public decimal ActualRate { get; private set; } public Guid RuleIdSnapshot { get; private set; } public long RuleVersionSnapshot { get; private set; }
    public decimal RuleRateSnapshot { get; private set; } public decimal LowerTolerancePercentSnapshot { get; private set; }
    public decimal UpperTolerancePercentSnapshot { get; private set; } public decimal RateVariance { get; private set; }
    internal static InputApplicationLine Create(Guid tenantId, Guid farmId, Guid applicationId, FieldReceiptLine receiptLine, StockIssueLine issueLine, InventoryApplicationRule rule, decimal coverage, decimal quantity) => new(tenantId, farmId, applicationId, receiptLine, issueLine, rule, coverage, quantity);
}
