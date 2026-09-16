namespace Cane360.Application.Administration;

public sealed record AdministrationRuleDto(
    Guid Id, Guid InventoryItemId, string ItemName,
    Guid ActivityTypeId, string ActivityTypeName, string UnitCode,
    string CoverageBasis, decimal RatePerCoverageUnit,
    decimal LowerTolerancePercent, decimal UpperTolerancePercent,
    DateOnly EffectiveFrom, DateOnly? EffectiveTo, long Version);
