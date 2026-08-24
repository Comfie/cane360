namespace Cane360.Domain.Inventory;

public sealed class ApprovalDecision : BaseEntity
{
    private ApprovalDecision() { }

    private ApprovalDecision(
        Guid tenantId,
        Guid farmId,
        Guid? stockReceiptId,
        Guid? inputRequestId,
        Guid? inventoryLossId,
        Guid? fieldAccountabilityCorrectionId,
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
        InputRequestId = inputRequestId;
        InventoryLossId = inventoryLossId;
        FieldAccountabilityCorrectionId = fieldAccountabilityCorrectionId;
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
    public Guid? StockReceiptId { get; private set; }
    public Guid? InputRequestId { get; private set; }
    public Guid? InventoryLossId { get; private set; }
    public Guid? FieldAccountabilityCorrectionId { get; private set; }
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
            tenantId, farmId, receiptId, null, null, null, subjectVersion, outcome,
            approverUserId, approverRole, decidedAt, reason, idempotencyKey);
    }

    public static ApprovalDecision CreateInputRequestDecision(
        Guid tenantId, Guid farmId, Guid inputRequestId, long subjectVersion,
        ApprovalOutcome outcome, string approverUserId, string approverRole,
        DateTimeOffset decidedAt, string? reason, string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approverUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approverRole);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (outcome == ApprovalOutcome.Rejected && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A rejection reason is required.");
        return new ApprovalDecision(tenantId, farmId, null, inputRequestId, null, null, subjectVersion,
            outcome, approverUserId, approverRole, decidedAt, reason, idempotencyKey);
    }

    public static ApprovalDecision CreateInventoryLossDecision(Guid tenantId, Guid farmId, Guid inventoryLossId,
        long subjectVersion, ApprovalOutcome outcome, string approverUserId, string approverRole,
        DateTimeOffset decidedAt, string? reason, string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approverUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approverRole);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (outcome == ApprovalOutcome.Rejected && string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("A rejection reason is required.");
        return new ApprovalDecision(tenantId, farmId, null, null, inventoryLossId, null, subjectVersion,
            outcome, approverUserId, approverRole, decidedAt, reason, idempotencyKey);
    }

    public static ApprovalDecision CreateFieldAccountabilityCorrectionDecision(
        Guid tenantId, Guid farmId, Guid correctionId, long subjectVersion, ApprovalOutcome outcome,
        string approverUserId, string approverRole, DateTimeOffset decidedAt, string? reason,
        string idempotencyKey)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(approverUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(approverRole);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (outcome == ApprovalOutcome.Rejected && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A rejection reason is required.");
        return new ApprovalDecision(tenantId, farmId, null, null, null, correctionId, subjectVersion,
            outcome, approverUserId, approverRole, decidedAt, reason, idempotencyKey);
    }
}
