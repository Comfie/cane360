using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record EditInputRequestLineCommand(
    Guid InputRequestId, Guid InputRequestLineId, decimal RequestedQuantity,
    long ExpectedVersion) : IRequest;
