namespace Cane360.Application.Payroll;

public sealed record ReopenPayrollSettlementInput(int CalculationVersion, string Reason, string IdempotencyKey);
