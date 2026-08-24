namespace Cane360.Application.Inventory;

public sealed record InputRequestLineDto(
    Guid Id, Guid InventoryItemId, string ItemCode, string ItemName, string UnitCode,
    Guid RuleId, long RuleVersion, string CoverageBasis, decimal PlannedCoverage,
    decimal PlannedRate, decimal PlannedQuantity, decimal RequestedQuantity,
    decimal MinimumQuantity, decimal MaximumQuantity, string ApprovalRequirement,
    decimal AvailableQuantitySnapshot, decimal LiveAvailableQuantity,
    decimal? EstimatedUnitCostUsdSnapshot, decimal? EstimatedValueUsdSnapshot,
    decimal AlreadyIssuedQuantity, decimal RemainingQuantity);
