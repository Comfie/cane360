namespace Cane360.Application.Payroll;

public sealed record CalculatePayrollRunCommand(Guid PayrollRunId, long ExpectedVersion) : IRequest<PayrollRunDto>;
