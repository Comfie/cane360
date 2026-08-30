namespace Cane360.Application.Payroll;

public sealed record SubmitPayrollRunCommand(Guid PayrollRunId, long ExpectedVersion, int CalculationVersion) : IRequest<PayrollRunDto>;
