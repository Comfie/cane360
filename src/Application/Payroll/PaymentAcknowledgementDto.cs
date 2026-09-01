namespace Cane360.Application.Payroll;

public sealed record PaymentAcknowledgementDto(Guid Id, string Status, Guid? AcknowledgedByPersonId,
    string CapturedByUserId, Guid? CapturedByPersonId, DateTimeOffset AcknowledgedAt,
    string? EvidenceReference, DateTimeOffset CreatedAt);
