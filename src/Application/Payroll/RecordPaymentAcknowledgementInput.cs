namespace Cane360.Application.Payroll;

public sealed record RecordPaymentAcknowledgementInput(string Status, Guid? AcknowledgedByPersonId,
    DateTimeOffset AcknowledgedAt, string? EvidenceReference, string IdempotencyKey);
