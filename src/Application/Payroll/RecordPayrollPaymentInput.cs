namespace Cane360.Application.Payroll;

public sealed record RecordPayrollPaymentInput(int CalculationVersion, Guid PayrollWorkerLineId,
    string Method, decimal AmountUsd, DateOnly PaymentDate, string? Provider,
    string? RecipientNumber, string? TransactionReference, string? ExternalStatus,
    string IdempotencyKey);
