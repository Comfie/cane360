namespace Cane360.Web.Models.Payroll;

public sealed record ClosePayrollSettlementRequest(int CalculationVersion, string IdempotencyKey);
