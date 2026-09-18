using Cane360.Application.Common.Security;
namespace Cane360.Application.Payroll;

[Authorize(TenantRoles = TenantSecurityRoles.FarmManager)]
public sealed record CreatePayrollRunCommand(Guid PayrollPeriodId) : IRequest<PayrollRunDto>;
