namespace Cane360.Domain.Inventory;

public sealed class FieldReceipt : BaseAuditableEntity
{
    private readonly List<FieldReceiptLine> _lines = [];
    private FieldReceipt() { }

    private FieldReceipt(Guid tenantId, Guid farmId, StockIssue issue, Guid fieldId, Guid cropCycleId,
        Guid activityId, Guid recipientPersonId, DateTimeOffset receivedAt, DateTimeOffset enteredAt,
        string enteredByUserId, string? lateEntryReason, int entryDelayDays)
    {
        if (entryDelayDays > 2 && string.IsNullOrWhiteSpace(lateEntryReason))
            throw new InvalidOperationException("A late-entry reason is required when a field receipt is entered more than two calendar days later.");
        TenantId = tenantId; FarmId = farmId; StockIssueId = issue.Id; FieldId = fieldId;
        CropCycleId = cropCycleId; ActivityId = activityId; RecipientPersonId = recipientPersonId;
        ReceivedAt = receivedAt; EnteredAt = enteredAt; EnteredByUserId = enteredByUserId.Trim();
        LateEntryReason = string.IsNullOrWhiteSpace(lateEntryReason) ? null : lateEntryReason.Trim();
        EntryDelayDays = entryDelayDays; Status = FieldReceiptStatus.Recorded; Version = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StockIssueId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid CropCycleId { get; private set; }
    public Guid ActivityId { get; private set; }
    public Guid RecipientPersonId { get; private set; }
    public DateTimeOffset ReceivedAt { get; private set; }
    public DateTimeOffset EnteredAt { get; private set; }
    public string EnteredByUserId { get; private set; } = string.Empty;
    public string? LateEntryReason { get; private set; }
    public int EntryDelayDays { get; private set; }
    public FieldReceiptStatus Status { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<FieldReceiptLine> Lines => _lines.AsReadOnly();

    public static FieldReceipt Create(Guid tenantId, Guid farmId, StockIssue issue, Guid fieldId,
        Guid cropCycleId, Guid activityId, Guid recipientPersonId, DateTimeOffset receivedAt,
        DateTimeOffset enteredAt, string enteredByUserId, string? lateEntryReason, int entryDelayDays) =>
        new(tenantId, farmId, issue, fieldId, cropCycleId, activityId, recipientPersonId, receivedAt,
            enteredAt, enteredByUserId, lateEntryReason, entryDelayDays);

    public FieldReceiptLine AddLine(StockIssueLine issueLine, decimal quantity)
    {
        if (Status != FieldReceiptStatus.Recorded) throw new InvalidOperationException("Only a recorded field receipt may retain its original lines.");
        if (_lines.Any(line => line.StockIssueLineId == issueLine.Id)) throw new InvalidOperationException("An issue line may appear only once in a field receipt.");
        var line = FieldReceiptLine.Create(TenantId, FarmId, Id, issueLine, quantity);
        _lines.Add(line); return line;
    }

    public void Supersede(long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This field receipt changed after it was loaded. Refresh and try again.");
        if (Status != FieldReceiptStatus.Recorded) throw new InvalidOperationException("Only a recorded field receipt can be superseded.");
        Status = FieldReceiptStatus.Superseded; Version++;
    }
}
