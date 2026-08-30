namespace Cane360.Application.Payroll;

public sealed record GetPayrollRunQuery(Guid PayrollRunId) : IRequest<PayrollRunDto>;
