namespace Cane360.Domain.Inventory;

public sealed class StockReceipt : BaseAuditableEntity
{
    private readonly List<StockReceiptLine> _lines = [];

    private StockReceipt() { }

    private StockReceipt(
        Guid tenantId,
        Guid farmId,
        Guid storeId,
        StockReceiptType receiptType,
        Guid? supplierId,
        DateOnly receiptDate,
        Guid? receivedByPersonId,
        string sourceReference,
        string? reason,
        string? lateEntryReason)
    {
        TenantId = tenantId;
        FarmId = farmId;
        StoreId = storeId;
        ReceiptType = receiptType;
        SupplierId = supplierId;
        ReceiptDate = receiptDate;
        ReceivedByPersonId = receivedByPersonId;
        SourceReference = sourceReference.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        LateEntryReason = string.IsNullOrWhiteSpace(lateEntryReason) ? null : lateEntryReason.Trim();
        Status = StockReceiptStatus.Draft;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StoreId { get; private set; }
    public StockReceiptType ReceiptType { get; private set; }
    public Guid? SupplierId { get; private set; }
    public DateOnly ReceiptDate { get; private set; }
    public Guid? ReceivedByPersonId { get; private set; }
    public string SourceReference { get; private set; } = string.Empty;
    public string? Reason { get; private set; }
    public string? LateEntryReason { get; private set; }
    public StockReceiptStatus Status { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public string? PostedByUserId { get; private set; }
    public string? PostingIdempotencyKey { get; private set; }
    public DateTimeOffset? ReversedAt { get; private set; }
    public string? ReversedByUserId { get; private set; }
    public string? ReversalIdempotencyKey { get; private set; }
    public Guid? CorrectsStockReceiptId { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<StockReceiptLine> Lines => _lines.AsReadOnly();

    public static StockReceipt Create(
        Guid tenantId,
        Guid farmId,
        Guid storeId,
        StockReceiptType receiptType,
        Guid? supplierId,
        DateOnly receiptDate,
        Guid? receivedByPersonId,
        string sourceReference,
        string? reason,
        string? lateEntryReason,
        int entryDelayDays,
        Guid? correctsStockReceiptId = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceReference);
        if (receiptType == StockReceiptType.Purchase && supplierId is null)
        {
            throw new InvalidOperationException("A purchase receipt requires a supplier.");
        }
        if (receiptType == StockReceiptType.OpeningBalance && supplierId is not null)
        {
            throw new InvalidOperationException("An opening balance cannot have a supplier.");
        }
        if (receiptType == StockReceiptType.OpeningBalance && string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("An opening balance requires a reason.");
        }
        if (entryDelayDays < 0)
        {
            throw new InvalidOperationException("A receipt date cannot be in the future.");
        }
        if (entryDelayDays > 2 && string.IsNullOrWhiteSpace(lateEntryReason))
        {
            throw new InvalidOperationException("A late-entry reason is required after two calendar days.");
        }

        return new StockReceipt(
            tenantId, farmId, storeId, receiptType, supplierId, receiptDate,
            receivedByPersonId, sourceReference, reason, lateEntryReason)
        {
            CorrectsStockReceiptId = correctsStockReceiptId
        };
    }

    public StockReceiptLine AddLine(
        InventoryItem item,
        InventoryLot? lot,
        decimal quantity,
        decimal unitCostUsd,
        long expectedVersion)
    {
        RequireVersion(expectedVersion);
        EnsureDraft();
        if (item.TenantId != TenantId || item.FarmId != FarmId || item.Status != InventoryRecordStatus.Active)
        {
            throw new InvalidOperationException("The inventory item must be active on this farm.");
        }
        if (quantity <= 0) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (unitCostUsd < 0) throw new ArgumentOutOfRangeException(nameof(unitCostUsd));
        if (item.LotTrackingPolicy == LotTrackingPolicy.Required && lot is null)
        {
            throw new InvalidOperationException("This item requires a lot.");
        }
        if (item.LotTrackingPolicy == LotTrackingPolicy.None && lot is not null)
        {
            throw new InvalidOperationException("This item does not use lots.");
        }
        if (lot is not null &&
            (lot.InventoryItemId != item.Id || lot.FarmId != FarmId || lot.Status != InventoryRecordStatus.Active))
        {
            throw new InvalidOperationException("The lot must be active and belong to this item and farm.");
        }

        var line = StockReceiptLine.Create(
            Id, TenantId, FarmId, _lines.Count + 1, item, lot, quantity, unitCostUsd);
        _lines.Add(line);
        Version++;
        return line;
    }

    public void SubmitOpeningBalance(long expectedVersion)
    {
        RequireVersion(expectedVersion);
        EnsureDraft();
        EnsureHasLines();
        if (ReceiptType != StockReceiptType.OpeningBalance)
        {
            throw new InvalidOperationException("Only opening balances require submission for approval.");
        }
        Status = StockReceiptStatus.PendingApproval;
        Version++;
    }

    public void RecordOpeningDecision(ApprovalOutcome outcome, long subjectVersion)
    {
        RequireVersion(subjectVersion);
        if (ReceiptType != StockReceiptType.OpeningBalance || Status != StockReceiptStatus.PendingApproval)
        {
            throw new InvalidOperationException("This opening balance is not awaiting approval.");
        }
        Status = outcome == ApprovalOutcome.Approved
            ? StockReceiptStatus.Approved
            : StockReceiptStatus.Cancelled;
        Version++;
    }

    public void MarkPosted(DateTimeOffset postedAt, string postedByUserId, string idempotencyKey, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(postedByUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        EnsureHasLines();
        if (ReceiptType == StockReceiptType.Purchase && Status != StockReceiptStatus.Draft ||
            ReceiptType == StockReceiptType.OpeningBalance && Status != StockReceiptStatus.Approved)
        {
            throw new InvalidOperationException("The receipt is not ready to post.");
        }
        Status = StockReceiptStatus.Posted;
        PostedAt = postedAt;
        PostedByUserId = postedByUserId.Trim();
        PostingIdempotencyKey = idempotencyKey.Trim();
        Version++;
    }

    public void MarkReversed(DateTimeOffset reversedAt, string reversedByUserId, string idempotencyKey, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(reversedByUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (Status != StockReceiptStatus.Posted)
        {
            throw new InvalidOperationException("Only a posted receipt can be reversed.");
        }
        Status = StockReceiptStatus.Reversed;
        ReversedAt = reversedAt;
        ReversedByUserId = reversedByUserId.Trim();
        ReversalIdempotencyKey = idempotencyKey.Trim();
        Version++;
    }

    public bool IsPostingRetry(string idempotencyKey) =>
        Status == StockReceiptStatus.Posted && PostingIdempotencyKey == idempotencyKey;

    public bool IsReversalRetry(string idempotencyKey) =>
        Status == StockReceiptStatus.Reversed && ReversalIdempotencyKey == idempotencyKey;

    private void EnsureDraft()
    {
        if (Status != StockReceiptStatus.Draft)
        {
            throw new InvalidOperationException("Only a draft receipt can be changed.");
        }
    }

    private void EnsureHasLines()
    {
        if (_lines.Count == 0) throw new InvalidOperationException("At least one receipt line is required.");
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This receipt changed after it was loaded. Refresh and try again.");
        }
    }
}
