using Cane360.Application.Common.Security;
namespace Cane360.Application.Payroll;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record SubmitPayrollRunCommand(Guid PayrollRunId, long ExpectedVersion, int CalculationVersion) : IRequest<PayrollRunDto>;
