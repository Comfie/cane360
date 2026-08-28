using Cane360.Domain.Payroll;

namespace Cane360.Web.Models.Payroll;

public sealed record IssueWorkerAdvanceRequest(long ExpectedVersion, AdvancePaymentMethod PaymentMethod, decimal AmountUsd, DateTimeOffset IssuedAt, Guid? PayingPersonId, bool? WorkerAcknowledged, string? Provider, string? RecipientNumber, string? ExternalReference, string? TransactionStatus, string IdempotencyKey);
