namespace Cane360.Web.Models.Payroll;

public sealed record RecordPayrollPaymentRequest(int CalculationVersion, Guid PayrollWorkerLineId,
    string Method, decimal AmountUsd, string PaymentDate, string? Provider, string? RecipientNumber,
    string? TransactionReference, string? ExternalStatus, string IdempotencyKey);
