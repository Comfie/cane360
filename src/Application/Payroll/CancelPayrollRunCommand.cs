namespace Cane360.Application.Payroll;

public sealed record CancelPayrollRunCommand(Guid PayrollRunId, long ExpectedVersion, string Reason) : IRequest<PayrollRunDto>;
