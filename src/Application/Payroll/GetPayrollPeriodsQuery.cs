namespace Cane360.Application.Payroll;

public sealed record GetPayrollPeriodsQuery : IRequest<IReadOnlyList<PayrollPeriodDto>>;
