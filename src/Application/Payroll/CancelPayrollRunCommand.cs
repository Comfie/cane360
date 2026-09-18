using Cane360.Application.Common.Security;
namespace Cane360.Application.Payroll;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record CancelPayrollRunCommand(Guid PayrollRunId, long ExpectedVersion, string Reason) : IRequest<PayrollRunDto>;
