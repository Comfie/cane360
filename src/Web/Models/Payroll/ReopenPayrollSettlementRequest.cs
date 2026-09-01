namespace Cane360.Web.Models.Payroll;

public sealed record ReopenPayrollSettlementRequest(int CalculationVersion, string Reason, string IdempotencyKey);
