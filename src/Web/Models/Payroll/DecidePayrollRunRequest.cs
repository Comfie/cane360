namespace Cane360.Web.Models.Payroll;

public sealed record DecidePayrollRunRequest(long ExpectedVersion, int CalculationVersion, bool Approved, string? Reason, string IdempotencyKey);
