using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CreateInventoryApplicationRuleCommand(
    Guid InventoryItemId,
    Guid ActivityTypeId,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo,
    string CoverageBasis,
    decimal RatePerCoverageUnit,
    decimal LowerTolerancePercent,
    decimal UpperTolerancePercent) : IRequest<InventoryApplicationRuleDto>;
