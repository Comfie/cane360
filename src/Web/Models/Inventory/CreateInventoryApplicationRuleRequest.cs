namespace Cane360.Web.Models.Inventory;

public sealed record CreateInventoryApplicationRuleRequest(
    Guid InventoryItemId, Guid ActivityTypeId, DateOnly EffectiveFrom, DateOnly? EffectiveTo,
    string CoverageBasis, decimal RatePerCoverageUnit,
    decimal LowerTolerancePercent, decimal UpperTolerancePercent);
