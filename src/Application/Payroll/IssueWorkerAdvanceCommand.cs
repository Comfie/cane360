namespace Cane360.Application.Payroll;

public sealed record IssueWorkerAdvanceCommand(Guid AdvanceId, long ExpectedVersion, AdvancePaymentMethod PaymentMethod, decimal AmountUsd, DateTimeOffset IssuedAt, Guid? PayingPersonId, bool? WorkerAcknowledged, string? Provider, string? RecipientNumber, string? ExternalReference, string? TransactionStatus, string IdempotencyKey) : IRequest<WorkerAdvanceDto>;
