namespace Cane360.Application.Payroll;

public sealed record CreatePayrollPeriodCommand(int Year, int Month) : IRequest<PayrollPeriodDto>;
