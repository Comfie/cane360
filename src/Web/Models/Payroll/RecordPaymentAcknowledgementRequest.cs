namespace Cane360.Web.Models.Payroll;

public sealed record RecordPaymentAcknowledgementRequest(string Status, Guid? AcknowledgedByPersonId,
    DateTimeOffset AcknowledgedAt, string? EvidenceReference, string IdempotencyKey);
