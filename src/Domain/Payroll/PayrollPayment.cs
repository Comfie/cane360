namespace Cane360.Domain.Payroll;

public sealed class PayrollPayment : BaseEntity
{
    private PayrollPayment() { }

    private PayrollPayment(Guid id, Guid tenantId, Guid farmId, Guid runId, Guid calculationId,
        int calculationVersion, Guid workerLineId, Guid workerId, PayrollPaymentMethod method,
        decimal amountUsd, DateOnly paymentDate, string externalStatus, string? provider,
        byte[]? recipientCiphertext, byte[]? recipientNonce, byte[]? recipientTag,
        string? recipientKeyId, string? maskedRecipient, string? transactionReference,
        string userId, Guid? personId, DateTimeOffset createdAt, string idempotencyKey,
        string correlationId)
    {
        Id = id; TenantId = tenantId; FarmId = farmId; PayrollRunId = runId;
        PayrollCalculationId = calculationId; CalculationVersion = calculationVersion;
        PayrollWorkerLineId = workerLineId; WorkerProfileId = workerId; Method = method;
        AmountUsd = amountUsd; PaymentDate = paymentDate; ExternalStatus = externalStatus;
        Provider = provider; RecipientCiphertext = recipientCiphertext; RecipientNonce = recipientNonce;
        RecipientTag = recipientTag; RecipientKeyId = recipientKeyId; MaskedRecipientNumber = maskedRecipient;
        TransactionReference = transactionReference; RecordedByUserId = userId.Trim();
        RecordedByPersonId = personId; CreatedAt = createdAt; IdempotencyKey = idempotencyKey.Trim();
        CorrelationId = correlationId.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public Guid PayrollCalculationId { get; private set; }
    public int CalculationVersion { get; private set; }
    public Guid PayrollWorkerLineId { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public PayrollPaymentMethod Method { get; private set; }
    public decimal AmountUsd { get; private set; }
    public DateOnly PaymentDate { get; private set; }
    public string ExternalStatus { get; private set; } = string.Empty;
    public string? Provider { get; private set; }
    public byte[]? RecipientCiphertext { get; private set; }
    public byte[]? RecipientNonce { get; private set; }
    public byte[]? RecipientTag { get; private set; }
    public string? RecipientKeyId { get; private set; }
    public string? MaskedRecipientNumber { get; private set; }
    public string? TransactionReference { get; private set; }
    public string RecordedByUserId { get; private set; } = string.Empty;
    public Guid? RecordedByPersonId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public bool ContributesToPaidAmount => Method == PayrollPaymentMethod.Cash || ExternalStatus is "Posted" or "Successful";

    public static PayrollPayment Cash(Guid id, Guid tenantId, Guid farmId, Guid runId,
        Guid calculationId, int calculationVersion, Guid workerLineId, Guid workerId,
        decimal amountUsd, DateOnly paymentDate, string userId, Guid? personId,
        DateTimeOffset createdAt, string idempotencyKey, string correlationId)
    {
        ValidateShared(id, tenantId, farmId, runId, calculationId, calculationVersion, workerLineId,
            workerId, amountUsd, paymentDate, userId, idempotencyKey, correlationId);
        return new(id, tenantId, farmId, runId, calculationId, calculationVersion, workerLineId,
            workerId, PayrollPaymentMethod.Cash, amountUsd, paymentDate, "Posted", null, null,
            null, null, null, null, null, userId, personId, createdAt, idempotencyKey, correlationId);
    }

    public static PayrollPayment MobileMoney(Guid id, Guid tenantId, Guid farmId, Guid runId,
        Guid calculationId, int calculationVersion, Guid workerLineId, Guid workerId,
        decimal amountUsd, DateOnly paymentDate, string externalStatus, string provider,
        byte[] recipientCiphertext, byte[] recipientNonce, byte[] recipientTag, string recipientKeyId,
        string maskedRecipient, string transactionReference, string userId, Guid? personId,
        DateTimeOffset createdAt, string idempotencyKey, string correlationId)
    {
        ValidateShared(id, tenantId, farmId, runId, calculationId, calculationVersion, workerLineId,
            workerId, amountUsd, paymentDate, userId, idempotencyKey, correlationId);
        if (string.IsNullOrWhiteSpace(provider) || string.IsNullOrWhiteSpace(transactionReference) ||
            string.IsNullOrWhiteSpace(maskedRecipient) || string.IsNullOrWhiteSpace(recipientKeyId) ||
            recipientCiphertext.Length == 0 || recipientNonce.Length != 12 || recipientTag.Length != 16 ||
            externalStatus is not ("Posted" or "Successful" or "Pending" or "Failed"))
            throw new ArgumentException("Mobile-money payment requires provider, protected recipient, reference, date, amount, and a valid status.");
        return new(id, tenantId, farmId, runId, calculationId, calculationVersion, workerLineId,
            workerId, PayrollPaymentMethod.MobileMoney, amountUsd, paymentDate, externalStatus,
            provider.Trim(), recipientCiphertext, recipientNonce, recipientTag, recipientKeyId.Trim(),
            maskedRecipient.Trim(), transactionReference.Trim(), userId, personId, createdAt,
            idempotencyKey, correlationId);
    }

    private static void ValidateShared(Guid id, Guid tenantId, Guid farmId, Guid runId,
        Guid calculationId, int calculationVersion, Guid workerLineId, Guid workerId,
        decimal amountUsd, DateOnly paymentDate, string userId, string idempotencyKey, string correlationId)
    {
        if (new[] { id, tenantId, farmId, runId, calculationId, workerLineId, workerId }.Any(x => x == Guid.Empty))
            throw new ArgumentException("Exact payroll and worker identity is required.");
        if (calculationVersion <= 0 || amountUsd <= 0 || paymentDate == default)
            throw new ArgumentException("Calculation version, payment date, and positive USD amount are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
    }
}
