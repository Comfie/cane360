using Cane360.Application.Common.Security;
namespace Cane360.Application.Payroll;

[Authorize(TenantRoles = TenantSecurityRoles.Grower)]
public sealed record DecidePayrollRunCommand(Guid PayrollRunId, long ExpectedVersion, int CalculationVersion, bool Approved, string? Reason, string IdempotencyKey) : IRequest<PayrollRunDto>;
