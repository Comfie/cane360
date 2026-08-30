namespace Cane360.Application.Payroll;

public sealed record CreatePayrollRunCommand(Guid PayrollPeriodId) : IRequest<PayrollRunDto>;
