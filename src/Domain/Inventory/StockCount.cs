namespace Cane360.Domain.Inventory;

public sealed class StockCount : BaseAuditableEntity
{
    private StockCount() { }

    private StockCount(Guid tenantId, Guid farmId, Guid storeId, string notes, string countingPersons,
        DateOnly eventDate, string createdByUserId)
    {
        TenantId = tenantId;
        FarmId = farmId;
        StoreId = storeId;
        Notes = notes.Trim();
        CountingPersons = countingPersons.Trim();
        EventDate = eventDate;
        CreatedByUserId = createdByUserId.Trim();
        Status = StockCountStatus.Draft;
        Version = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StoreId { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public string CountingPersons { get; private set; } = string.Empty;
    public DateOnly EventDate { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public StockCountStatus Status { get; private set; }
    public long? CutoffPostingSequence { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? ReviewedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public long Version { get; private set; }
    public ICollection<StockCountLine> Lines { get; } = new List<StockCountLine>();

    public static StockCount Create(Guid tenantId, Guid farmId, Guid storeId, string notes,
        string countingPersons, DateOnly eventDate, string createdByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(countingPersons);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        return new StockCount(tenantId, farmId, storeId, notes, countingPersons, eventDate, createdByUserId);
    }

    public void Start(long cutoffPostingSequence, IEnumerable<StockCountLine> lines, DateTimeOffset startedAt,
        long expectedVersion)
    {
        Require(expectedVersion);
        if (Status != StockCountStatus.Draft) throw new InvalidOperationException("Only a draft count can be started.");
        if (cutoffPostingSequence < 0) throw new InvalidOperationException("The count cut-off is invalid.");
        CutoffPostingSequence = cutoffPostingSequence;
        foreach (var line in lines) Lines.Add(line);
        StartedAt = startedAt;
        Status = StockCountStatus.InProgress;
        Version++;
    }

    public void MoveToReview(DateTimeOffset reviewedAt, long expectedVersion)
    {
        Require(expectedVersion);
        if (Status != StockCountStatus.InProgress) throw new InvalidOperationException("Only an in-progress count can be reviewed.");
        ReviewedAt = reviewedAt;
        Status = StockCountStatus.Review;
        Version++;
    }

    public void ResolveReview(DateTimeOffset closedAt, long expectedVersion)
    {
        Require(expectedVersion);
        if (Status != StockCountStatus.Review) throw new InvalidOperationException("Only a review count can be resolved.");
        Status = Lines.All(line => line.VarianceQuantity == 0) ? StockCountStatus.ClosedNoVariance : StockCountStatus.PendingAdjustment;
        if (Status == StockCountStatus.ClosedNoVariance) ClosedAt = closedAt;
        Version++;
    }

    public void CloseAfterAdjustments(DateTimeOffset closedAt)
    {
        if (Status != StockCountStatus.PendingAdjustment) throw new InvalidOperationException("Only a count pending adjustments can close.");
        if (Lines.Any(line => line.VarianceQuantity != 0 && !line.IsResolved)) throw new InvalidOperationException("Every non-zero variance requires a posted adjustment.");
        Status = StockCountStatus.Closed;
        ClosedAt = closedAt;
        Version++;
    }

    public void ReopenAfterAdjustmentReversal(Guid adjustmentId)
    {
        if (Status != StockCountStatus.Closed || !Lines.Any(line => line.PostedStockAdjustmentId == adjustmentId))
            throw new InvalidOperationException("Only the posted count adjustment can reopen this count.");
        Status = StockCountStatus.PendingAdjustment;
        ClosedAt = null;
        Version++;
    }

    public void Cancel(string reason, long expectedVersion)
    {
        Require(expectedVersion);
        if (Status is not (StockCountStatus.Draft or StockCountStatus.InProgress)) throw new InvalidOperationException("Only a draft or in-progress count can be cancelled.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        CancellationReason = reason.Trim();
        Status = StockCountStatus.Cancelled;
        Version++;
    }

    private void Require(long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This count changed after it was loaded. Refresh and try again.");
    }
}
