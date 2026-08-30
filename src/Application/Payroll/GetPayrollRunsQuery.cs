namespace Cane360.Application.Payroll;

public sealed record GetPayrollRunsQuery : IRequest<IReadOnlyList<PayrollRunDto>>;
