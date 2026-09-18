using Cane360.Application.Common.Security;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

[Authorize(TenantRoles = TenantSecurityRoles.Grower + "," + TenantSecurityRoles.FarmManager)]
public sealed record CreateInputApplicationCommand(Guid ActivityId, DateTimeOffset AppliedAt,
    ApplicationCoverageBasis CoverageBasis, decimal VerifiedCoverage,
    IReadOnlyList<CreateInputApplicationLineCommand> Lines) : IRequest<Guid>;
