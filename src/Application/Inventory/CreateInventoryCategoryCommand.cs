using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record CreateInventoryCategoryCommand(
    string Code, string Name, string? Description, int DisplayOrder) : IRequest<InventoryCategoryDto>;
