using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record UpdateInventoryCategoryCommand(
    Guid CategoryId, string Name, string? Description, int DisplayOrder, long ExpectedVersion) : IRequest<InventoryCategoryDto>;
