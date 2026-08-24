namespace Cane360.Application.Inventory;

public sealed record InventoryApplicationRuleDto(
    Guid Id, Guid InventoryItemId, Guid ActivityTypeId, DateOnly EffectiveFrom,
    DateOnly? EffectiveTo, string CoverageBasis, decimal RatePerCoverageUnit,
    decimal LowerTolerancePercent, decimal UpperTolerancePercent, string UnitCode, long Version);
