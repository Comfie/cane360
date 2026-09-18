using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record CreateManagerInvitationCommand(Guid PersonId, int ExpiresInHours, string Role)
    : IRequest<CreatedManagerInvitationDto>;
