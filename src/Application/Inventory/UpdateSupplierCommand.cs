using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record UpdateSupplierCommand(Guid SupplierId, string Code, string Name,
    string? Contact, long ExpectedVersion) : IRequest<SupplierDto>;
