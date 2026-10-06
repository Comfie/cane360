using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record SetInventoryCategoryActiveCommand(
    Guid CategoryId, bool Active, long ExpectedVersion) : IRequest<InventoryCategoryDto>;
