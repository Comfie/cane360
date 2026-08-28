namespace Cane360.Domain.Payroll;

public sealed class AdvanceIssue : BaseEntity
{
    private AdvanceIssue() { }
    private AdvanceIssue(Guid advanceId, Guid tenantId, Guid farmId, AdvancePaymentMethod method, decimal amountUsd, DateTimeOffset issuedAt, string recordedByUserId, Guid? payingPersonId, Guid? receivingWorkerId, bool? workerAcknowledged, string? provider, string? maskedRecipientNumber, string? externalReference, string? transactionStatus, string idempotencyKey)
    { WorkerAdvanceId = advanceId; TenantId = tenantId; FarmId = farmId; PaymentMethod = method; AmountUsd = amountUsd; IssuedAt = issuedAt; RecordedByUserId = recordedByUserId.Trim(); PayingPersonId = payingPersonId; ReceivingWorkerId = receivingWorkerId; WorkerAcknowledged = workerAcknowledged; Provider = provider; MaskedRecipientNumber = maskedRecipientNumber; ExternalReference = externalReference; TransactionStatus = transactionStatus; IdempotencyKey = idempotencyKey.Trim(); }
    public Guid WorkerAdvanceId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public AdvancePaymentMethod PaymentMethod { get; private set; }
    public decimal AmountUsd { get; private set; }
    public DateTimeOffset IssuedAt { get; private set; }
    public string RecordedByUserId { get; private set; } = string.Empty;
    public Guid? PayingPersonId { get; private set; }
    public Guid? ReceivingWorkerId { get; private set; }
    public bool? WorkerAcknowledged { get; private set; }
    public string? Provider { get; private set; }
    public string? MaskedRecipientNumber { get; private set; }
    public string? ExternalReference { get; private set; }
    public string? TransactionStatus { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public static AdvanceIssue Cash(Guid advanceId, Guid tenantId, Guid farmId, decimal amountUsd, DateTimeOffset issuedAt, string userId, Guid payingPersonId, Guid workerId, bool acknowledged, string idempotencyKey)
    { ValidateShared(amountUsd, issuedAt, userId, idempotencyKey); if (payingPersonId == Guid.Empty || workerId == Guid.Empty || !acknowledged) throw new InvalidOperationException("Cash issue evidence requires paying person, receiving worker, and acknowledgement."); return new(advanceId, tenantId, farmId, AdvancePaymentMethod.Cash, amountUsd, issuedAt, userId, payingPersonId, workerId, true, null, null, null, null, idempotencyKey); }
    public static AdvanceIssue MobileMoney(Guid advanceId, Guid tenantId, Guid farmId, decimal amountUsd, DateTimeOffset issuedAt, string userId, string provider, string maskedRecipientNumber, string externalReference, string transactionStatus, string idempotencyKey)
    { ValidateShared(amountUsd, issuedAt, userId, idempotencyKey); if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(maskedRecipientNumber) || string.IsNullOrWhiteSpace(externalReference) || string.IsNullOrWhiteSpace(transactionStatus)) throw new InvalidOperationException("Mobile Money issue evidence requires provider, masked recipient, reference, date, amount, and status."); return new(advanceId, tenantId, farmId, AdvancePaymentMethod.MobileMoney, amountUsd, issuedAt, userId, null, null, null, provider.Trim(), maskedRecipientNumber.Trim(), externalReference.Trim(), transactionStatus.Trim(), idempotencyKey); }
    private static void ValidateShared(decimal amountUsd, DateTimeOffset issuedAt, string userId, string idempotencyKey)
    { if (amountUsd <= 0 || issuedAt == default) throw new InvalidOperationException("Issue date, time, and positive USD amount are required."); ArgumentException.ThrowIfNullOrWhiteSpace(userId); ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey); }
}
