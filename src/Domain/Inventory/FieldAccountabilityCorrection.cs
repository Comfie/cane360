namespace Cane360.Domain.Inventory;

public sealed class FieldAccountabilityCorrection : BaseAuditableEntity
{
    private FieldAccountabilityCorrection() { }

    private FieldAccountabilityCorrection(Guid tenantId, Guid farmId, Guid activityId, Guid? fieldReceiptId,
        Guid? inputApplicationId, Guid? stockReturnId, Guid? inventoryLossId, long sourceVersion,
        string reason, string requestedByUserId, string idempotencyKey, DateTimeOffset requestedAt)
    {
        if (new[] { fieldReceiptId, inputApplicationId, stockReturnId, inventoryLossId }.Count(id => id.HasValue) != 1)
            throw new InvalidOperationException("A correction must identify exactly one typed original record.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason); ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserId); ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        TenantId = tenantId; FarmId = farmId; ActivityId = activityId; FieldReceiptId = fieldReceiptId;
        InputApplicationId = inputApplicationId; StockReturnId = stockReturnId; InventoryLossId = inventoryLossId;
        SourceVersion = sourceVersion; Reason = reason.Trim(); RequestedByUserId = requestedByUserId.Trim();
        RequestIdempotencyKey = idempotencyKey.Trim(); RequestedAt = requestedAt; Status = FieldAccountabilityCorrectionStatus.Requested; Version = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid ActivityId { get; private set; }
    public Guid? FieldReceiptId { get; private set; }
    public Guid? InputApplicationId { get; private set; }
    public Guid? StockReturnId { get; private set; }
    public Guid? InventoryLossId { get; private set; }
    public long SourceVersion { get; private set; }
    public string Reason { get; private set; } = string.Empty;
    public string RequestedByUserId { get; private set; } = string.Empty;
    public string RequestIdempotencyKey { get; private set; } = string.Empty;
    public DateTimeOffset RequestedAt { get; private set; }
    public FieldAccountabilityCorrectionStatus Status { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public DateTimeOffset? AppliedAt { get; private set; }
    public long Version { get; private set; }

    public static FieldAccountabilityCorrection ForFieldReceipt(Guid tenantId, Guid farmId, Guid activityId,
        Guid fieldReceiptId, long sourceVersion, string reason, string requestedBy, string key, DateTimeOffset at) =>
        new(tenantId, farmId, activityId, fieldReceiptId, null, null, null, sourceVersion, reason, requestedBy, key, at);
    public static FieldAccountabilityCorrection ForApplication(Guid tenantId, Guid farmId, Guid activityId,
        Guid applicationId, long sourceVersion, string reason, string requestedBy, string key, DateTimeOffset at) =>
        new(tenantId, farmId, activityId, null, applicationId, null, null, sourceVersion, reason, requestedBy, key, at);
    public static FieldAccountabilityCorrection ForReturn(Guid tenantId, Guid farmId, Guid activityId,
        Guid stockReturnId, long sourceVersion, string reason, string requestedBy, string key, DateTimeOffset at) =>
        new(tenantId, farmId, activityId, null, null, stockReturnId, null, sourceVersion, reason, requestedBy, key, at);
    public static FieldAccountabilityCorrection ForLoss(Guid tenantId, Guid farmId, Guid activityId,
        Guid lossId, long sourceVersion, string reason, string requestedBy, string key, DateTimeOffset at) =>
        new(tenantId, farmId, activityId, null, null, null, lossId, sourceVersion, reason, requestedBy, key, at);
    public void Decide(ApprovalOutcome outcome, DateTimeOffset at, long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This correction changed after it was loaded. Refresh and try again.");
        if (Status != FieldAccountabilityCorrectionStatus.Requested) throw new InvalidOperationException("Only a requested correction can be decided.");
        Status = outcome == ApprovalOutcome.Approved ? FieldAccountabilityCorrectionStatus.Approved : FieldAccountabilityCorrectionStatus.Rejected; DecidedAt = at; Version++;
    }
    public void MarkApplied(DateTimeOffset at, long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This correction changed after it was loaded. Refresh and try again.");
        if (Status != FieldAccountabilityCorrectionStatus.Approved) throw new InvalidOperationException("Only an approved correction can be applied.");
        Status = FieldAccountabilityCorrectionStatus.Applied; AppliedAt = at; Version++;
    }
}
