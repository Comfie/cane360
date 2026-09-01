namespace Cane360.Application.Payroll;

public sealed record ReversePayrollPaymentInput(decimal AmountUsd, string Reason, string IdempotencyKey);
