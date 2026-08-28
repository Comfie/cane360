namespace Cane360.Application.Payroll;

public sealed record AdvanceIssueDto(string PaymentMethod, decimal AmountUsd, DateTimeOffset IssuedAt, Guid? PayingPersonId, Guid? ReceivingWorkerId, bool? WorkerAcknowledged, string? Provider, string? MaskedRecipientNumber, string? ExternalReference, string? TransactionStatus);
