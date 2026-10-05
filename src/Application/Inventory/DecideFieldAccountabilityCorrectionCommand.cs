using Cane360.Application.Common.Security;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record DecideFieldAccountabilityCorrectionCommand(
    Guid CorrectionId,
    long ExpectedVersion,
    ApprovalOutcome Outcome,
    string? Reason,
    string IdempotencyKey) : IRequest;
