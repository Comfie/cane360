namespace Cane360.Domain.Inventory;

public sealed class StockReturn : BaseAuditableEntity
{
    private readonly List<StockReturnLine> _lines = [];
    private StockReturn() { }
    private StockReturn(Guid tenantId, Guid farmId, Guid storeId, Guid activityId, DateOnly returnDate, Guid senderPersonId, Guid receiverPersonId)
    { TenantId = tenantId; FarmId = farmId; StoreId = storeId; ActivityId = activityId; ReturnDate = returnDate; SenderPersonId = senderPersonId; ReceiverPersonId = receiverPersonId; Status = StockReturnStatus.Draft; Version = 1; }
    public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid StoreId { get; private set; } public Guid ActivityId { get; private set; }
    public DateOnly ReturnDate { get; private set; } public Guid SenderPersonId { get; private set; } public Guid ReceiverPersonId { get; private set; }
    public StockReturnStatus Status { get; private set; } public DateTimeOffset? PostedAt { get; private set; } public string? PostedByUserId { get; private set; }
    public string? PostingIdempotencyKey { get; private set; } public DateTimeOffset? ReversedAt { get; private set; } public string? ReversalIdempotencyKey { get; private set; }
    public long Version { get; private set; } public IReadOnlyCollection<StockReturnLine> Lines => _lines.AsReadOnly();
    public static StockReturn Create(Guid tenantId, Guid farmId, Guid storeId, Guid activityId, DateOnly returnDate, Guid senderPersonId, Guid receiverPersonId) => new(tenantId, farmId, storeId, activityId, returnDate, senderPersonId, receiverPersonId);
    public StockReturnLine AddLine(StockIssueLine issueLine, decimal quantity)
    { RequireDraft(); if (_lines.Any(line => line.StockIssueLineId == issueLine.Id)) throw new InvalidOperationException("An issue line may appear only once in a return."); var line = StockReturnLine.Create(TenantId, FarmId, Id, issueLine, quantity); _lines.Add(line); Version++; return line; }
    public void MarkPosted(DateTimeOffset postedAt, string userId, string idempotencyKey, long expectedVersion)
    { Require(expectedVersion); RequireDraft(); if (_lines.Count == 0) throw new InvalidOperationException("A stock return needs at least one line."); PostedAt = postedAt; PostedByUserId = userId.Trim(); PostingIdempotencyKey = idempotencyKey.Trim(); Status = StockReturnStatus.Posted; Version++; }
    public void MarkReversed(DateTimeOffset reversedAt, string idempotencyKey, long expectedVersion)
    { Require(expectedVersion); if (Status != StockReturnStatus.Posted) throw new InvalidOperationException("Only a posted return can be reversed."); ReversedAt = reversedAt; ReversalIdempotencyKey = idempotencyKey.Trim(); Status = StockReturnStatus.Reversed; Version++; }
    public bool IsPostingRetry(string key) => Status != StockReturnStatus.Draft && PostingIdempotencyKey == key;
    public bool IsReversalRetry(string key) => Status == StockReturnStatus.Reversed && ReversalIdempotencyKey == key;
    private void RequireDraft() { if (Status != StockReturnStatus.Draft) throw new InvalidOperationException("Only a draft return can be edited."); }
    private void Require(long version) { if (Version != version) throw new InvalidOperationException("This return changed after it was loaded. Refresh and try again."); }
}
