namespace Cane360.Web.Models.Payroll;

public sealed record ReversePayrollPaymentRequest(decimal AmountUsd, string Reason, string IdempotencyKey);
