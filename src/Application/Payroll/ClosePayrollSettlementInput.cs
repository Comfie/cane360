namespace Cane360.Application.Payroll;

public sealed record ClosePayrollSettlementInput(int CalculationVersion, string IdempotencyKey);
