namespace Cane360.Domain.Inventory;

public sealed class StockIssue : BaseAuditableEntity
{
    private readonly List<StockIssueLine> _lines = [];
    private StockIssue() { }

    private StockIssue(Guid tenantId, Guid farmId, Guid storeId, Guid inputRequestId,
        DateOnly issueDate, Guid issuerPersonId, Guid recipientPersonId, string? lateEntryReason, int entryDelayDays)
    {
        if (entryDelayDays > 2 && string.IsNullOrWhiteSpace(lateEntryReason))
            throw new InvalidOperationException("A late-entry reason is required when an issue is entered more than two calendar days later.");
        TenantId = tenantId;
        FarmId = farmId;
        StoreId = storeId;
        InputRequestId = inputRequestId;
        IssueDate = issueDate;
        IssuerPersonId = issuerPersonId;
        RecipientPersonId = recipientPersonId;
        LateEntryReason = string.IsNullOrWhiteSpace(lateEntryReason) ? null : lateEntryReason.Trim();
        EntryDelayDays = entryDelayDays;
        Status = StockIssueStatus.Draft;
        Version = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid InputRequestId { get; private set; }
    public DateOnly IssueDate { get; private set; }
    public Guid IssuerPersonId { get; private set; }
    public Guid RecipientPersonId { get; private set; }
    public string? LateEntryReason { get; private set; }
    public int EntryDelayDays { get; private set; }
    public StockIssueStatus Status { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public string? PostedByUserId { get; private set; }
    public string? PostingIdempotencyKey { get; private set; }
    public string? CorrectionReason { get; private set; }
    public string? CorrectionRequestedByUserId { get; private set; }
    public DateTimeOffset? ReversedAt { get; private set; }
    public string? ReversalIdempotencyKey { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<StockIssueLine> Lines => _lines.AsReadOnly();

    public static StockIssue Create(Guid tenantId, Guid farmId, Guid storeId, Guid requestId,
        DateOnly issueDate, Guid issuerPersonId, Guid recipientPersonId, string? lateEntryReason,
        int entryDelayDays) => new(tenantId, farmId, storeId, requestId, issueDate, issuerPersonId,
            recipientPersonId, lateEntryReason, entryDelayDays);

    public StockIssueLine AddLine(InputRequestLine requestLine, Guid stockPositionId,
        Guid? inventoryLotId, string? lotCode, decimal quantity, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != StockIssueStatus.Draft) throw new InvalidOperationException("Only a draft issue can be edited.");
        if (_lines.Any(line => line.InputRequestLineId == requestLine.Id && line.InventoryLotId == inventoryLotId))
            throw new InvalidOperationException("The request line and lot combination may appear only once per issue.");
        var line = StockIssueLine.Create(TenantId, FarmId, Id, _lines.Count + 1, requestLine,
            stockPositionId, inventoryLotId, lotCode, quantity);
        _lines.Add(line);
        Version++;
        return line;
    }

    public void MarkPosted(DateTimeOffset postedAt, string userId, string idempotencyKey, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (_lines.Count == 0) throw new InvalidOperationException("At least one issue line is required.");
        if (Status != StockIssueStatus.Draft) throw new InvalidOperationException("Only a draft issue can be posted.");
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        Status = StockIssueStatus.Posted;
        PostedAt = postedAt;
        PostedByUserId = userId.Trim();
        PostingIdempotencyKey = idempotencyKey.Trim();
        Version++;
    }

    public void RequestCorrection(string reason, string userId, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != StockIssueStatus.Posted) throw new InvalidOperationException("Only a posted issue can enter correction.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = StockIssueStatus.CorrectionRequested;
        CorrectionReason = reason.Trim();
        CorrectionRequestedByUserId = userId.Trim();
        Version++;
    }

    public void MarkReversed(DateTimeOffset reversedAt, string idempotencyKey, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is not (StockIssueStatus.Posted or StockIssueStatus.CorrectionRequested))
            throw new InvalidOperationException("Only a posted issue can be reversed.");
        Status = StockIssueStatus.Reversed;
        ReversedAt = reversedAt;
        ReversalIdempotencyKey = idempotencyKey.Trim();
        Version++;
    }

    public bool IsPostingRetry(string key) => Status != StockIssueStatus.Draft && PostingIdempotencyKey == key;
    public bool IsReversalRetry(string key) => Status == StockIssueStatus.Reversed && ReversalIdempotencyKey == key;

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This stock issue changed after it was loaded. Refresh and try again.");
    }
}
