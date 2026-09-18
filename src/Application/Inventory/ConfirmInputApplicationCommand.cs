using Cane360.Application.Common.Security;
namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record ConfirmInputApplicationCommand(Guid InputApplicationId, string? LateConfirmationReason, long ExpectedVersion, string IdempotencyKey) : IRequest;
