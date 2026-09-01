namespace Cane360.Domain.Payroll;

public sealed class PayrollPaymentReversal : BaseEntity
{
    private PayrollPaymentReversal() { }
    private PayrollPaymentReversal(Guid paymentId, Guid tenantId, Guid farmId, Guid runId,
        Guid calculationId, int calculationVersion, Guid workerLineId, decimal amountUsd,
        string reason, string userId, Guid? personId, DateTimeOffset reversedAt,
        string idempotencyKey, string correlationId)
    { PayrollPaymentId = paymentId; TenantId = tenantId; FarmId = farmId; PayrollRunId = runId; PayrollCalculationId = calculationId; CalculationVersion = calculationVersion; PayrollWorkerLineId = workerLineId; AmountUsd = amountUsd; Reason = reason.Trim(); ReversedByUserId = userId.Trim(); ReversedByPersonId = personId; ReversedAt = reversedAt; IdempotencyKey = idempotencyKey.Trim(); CorrelationId = correlationId.Trim(); }
    public Guid PayrollPaymentId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid PayrollRunId { get; private set; }
    public Guid PayrollCalculationId { get; private set; }
    public int CalculationVersion { get; private set; }
    public Guid PayrollWorkerLineId { get; private set; }
    public decimal AmountUsd { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string ReversedByUserId { get; private set; } = string.Empty;
    public Guid? ReversedByPersonId { get; private set; }
    public DateTimeOffset ReversedAt { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;
    public string CorrelationId { get; private set; } = string.Empty;
    public static PayrollPaymentReversal Create(PayrollPayment payment, decimal amountUsd,
        string reason, string userId, Guid? personId, DateTimeOffset at, string key, string correlationId)
    { if (amountUsd <= 0) throw new ArgumentException("A positive reversal amount is required."); ArgumentException.ThrowIfNullOrWhiteSpace(reason); ArgumentException.ThrowIfNullOrWhiteSpace(key); return new(payment.Id, payment.TenantId, payment.FarmId, payment.PayrollRunId, payment.PayrollCalculationId, payment.CalculationVersion, payment.PayrollWorkerLineId, amountUsd, reason, userId, personId, at, key, correlationId); }
}
