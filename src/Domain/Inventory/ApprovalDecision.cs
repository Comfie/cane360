namespace Cane360.Domain.Inventory;

public sealed class ApprovalDecision : BaseEntity
{
    private ApprovalDecision() { }

    private ApprovalDecision(
        Guid tenantId,
        Guid farmId,
        Guid stockReceiptId,
        long subjectVersion,
        ApprovalOutcome outcome,
        string approverUserId,
        string approverRole,
        DateTimeOffset decidedAt,
        string? reason,
        string idempotencyKey)
    {
        TenantId = tenantId;
        FarmId = farmId;
        StockReceiptId = stockReceiptId;
        SubjectVersion = subjectVersion;
        Outcome = outcome;
        ApproverUserId = approverUserId.Trim();
        ApproverRole = approverRole.Trim();
        DecidedAt = decidedAt;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        IdempotencyKey = idempotencyKey.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StockReceiptId { get; private set; }
    public long SubjectVersion { get; private set; }
    public ApprovalOutcome Outcome { get; private set; }
    public string ApproverUserId { get; private set; } = string.Empty;
    public string ApproverRole { get; private set; } = string.Empty;
    public DateTimeOffset DecidedAt { get; private set; }
    public string? Reason { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty;

    public static ApprovalDecision CreateOpeningBalanceDecision(
        Guid tenantId,
        Guid farmId,
        Guid receiptId,
        long subjectVersion,
        ApprovalOutcome outcome,
        string approverUserId,
        string approverRole,
        DateTimeOffset decidedAt,
        string? reason,
        string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approverUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approverRole);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (outcome == ApprovalOutcome.Rejected && string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }
        return new ApprovalDecision(
            tenantId, farmId, receiptId, subjectVersion, outcome,
            approverUserId, approverRole, decidedAt, reason, idempotencyKey);
    }
}
