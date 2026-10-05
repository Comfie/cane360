using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CreateInventoryLossCommand(
    Guid ActivityId,
    Guid StockIssueLineId,
    decimal Quantity,
    InventoryLossType LossType,
    string Reason) : IRequest<Guid>;
