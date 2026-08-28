namespace Cane360.Application.Payroll;

public sealed record OpenPayrollPeriodCommand(Guid PayrollPeriodId, long ExpectedVersion) : IRequest<PayrollPeriodDto>;
