namespace Cane360.Application.Inventory;

public sealed record CreateInventoryApplicationRuleCommand(
    Guid InventoryItemId,
    Guid ActivityTypeId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string CoverageBasis,
    decimal RatePerCoverageUnit,
    decimal LowerTolerancePercent,
    decimal UpperTolerancePercent) : IRequest<InventoryApplicationRuleDto>;
