using Cane360.Application.Common.Security;
namespace Cane360.Application.Payroll;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record DecideWorkerAdvanceCommand(Guid AdvanceId, long ExpectedVersion, bool Approved, string? Reason, string IdempotencyKey) : IRequest<WorkerAdvanceDto>;
