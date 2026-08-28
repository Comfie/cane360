namespace Cane360.Application.Payroll;

public sealed record CancelPayrollPeriodCommand(Guid PayrollPeriodId, long ExpectedVersion, string Reason) : IRequest<PayrollPeriodDto>;
