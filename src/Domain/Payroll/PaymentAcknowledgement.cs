namespace Cane360.Domain.Payroll;

public sealed class PaymentAcknowledgement : BaseEntity
{
    private PaymentAcknowledgement() { }
    private PaymentAcknowledgement(Guid paymentId, Guid tenantId, Guid farmId, string status,
        Guid? acknowledgedByPersonId, string capturedByUserId, Guid? capturedByPersonId,
        DateTimeOffset acknowledgedAt, string? evidenceReference, DateTimeOffset createdAt,
        string idempotencyKey, string correlationId)
    {
        PayrollPaymentId = paymentId; TenantId = tenantId; FarmId = farmId; Status = status;
        AcknowledgedByPersonId = acknowledgedByPersonId; CapturedByUserId = capturedByUserId.Trim();
        CapturedByPersonId = capturedByPersonId; AcknowledgedAt = acknowledgedAt;
        EvidenceReference = evidenceReference?.Trim(); CreatedAt = createdAt;
        IdempotencyKey = idempotencyKey.Trim(); CorrelationId = correlationId.Trim();
    }
    public Guid PayrollPaymentId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public string Status { get; private set; } = string.Empty;
    public Guid? AcknowledgedByPersonId { get; private set; }
    public string CapturedByUserId { get; private set; } = string.Empty;
    public Guid? CapturedByPersonId { get; private set; }
    public DateTimeOffset AcknowledgedAt { get; private set; }
    public string? EvidenceReference { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public bool IsComplete => Status == "Acknowledged";
    public static PaymentAcknowledgement Create(Guid paymentId, Guid tenantId, Guid farmId,
        string status, Guid? acknowledgedByPersonId, string capturedByUserId, Guid? capturedByPersonId,
        DateTimeOffset acknowledgedAt, string? evidenceReference, DateTimeOffset createdAt,
        string idempotencyKey, string correlationId)
    {
        if (paymentId == Guid.Empty || tenantId == Guid.Empty || farmId == Guid.Empty || acknowledgedAt == default)
            throw new ArgumentException("Payment identity and acknowledgement time are required.");
        if (status is not ("Acknowledged" or "Declined")) throw new ArgumentException("Acknowledgement status must be Acknowledged or Declined.");
        ArgumentException.ThrowIfNullOrWhiteSpace(capturedByUserId); ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        return new(paymentId, tenantId, farmId, status, acknowledgedByPersonId, capturedByUserId,
            capturedByPersonId, acknowledgedAt, evidenceReference, createdAt, idempotencyKey, correlationId);
    }
}
