namespace Cane360.Application.Payroll;

public sealed record DecidePayrollRunCommand(Guid PayrollRunId, long ExpectedVersion, int CalculationVersion, bool Approved, string? Reason, string IdempotencyKey) : IRequest<PayrollRunDto>;
