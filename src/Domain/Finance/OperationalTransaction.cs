namespace Cane360.Domain.Finance;

public sealed class OperationalTransaction : BaseEntity
{
    private readonly List<TransactionAllocation> _allocations = [];
    private OperationalTransaction() { }

    private OperationalTransaction(Guid tenantId, Guid farmId, OperationalTransactionType type,
        OperationalFinanceCategory category, DateOnly eventDate, string payeeOrPayer,
        decimal amountUsd, string? sourceReference, string? notes, string createdByUserId,
        DateTimeOffset createdAt, string correlationId)
    {
        ValidateAmount(amountUsd);
        ArgumentException.ThrowIfNullOrWhiteSpace(payeeOrPayer);
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        ArgumentException.ThrowIfNullOrWhiteSpace(correlationId);
        TenantId = tenantId;
        FarmId = farmId;
        Type = type;
        Category = category;
        EventDate = eventDate;
        PayeeOrPayer = payeeOrPayer.Trim();
        AmountUsd = amountUsd;
        SourceReference = Clean(sourceReference);
        Notes = Clean(notes);
        Status = OperationalTransactionStatus.Draft;
        CreatedByUserId = createdByUserId.Trim();
        CreatedAt = createdAt;
        CorrelationId = correlationId.Trim();
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public OperationalTransactionType Type { get; private set; }
    public OperationalFinanceCategory Category { get; private set; }
    public DateOnly EventDate { get; private set; }
    public string PayeeOrPayer { get; private set; } = string.Empty;
    public decimal AmountUsd { get; private set; }
    public string? SourceReference { get; private set; }
    public string? Notes { get; private set; }
    public OperationalTransactionStatus Status { get; private set; }
    public long Version { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; private set; }
    public string? PostedByUserId { get; private set; }
    public DateTimeOffset? PostedAt { get; private set; }
    public string? PostedIdempotencyKey { get; private set; }
    public bool IsClosedCycleCorrection { get; private set; }
    public string? ClosedCycleCorrectionReason { get; private set; }
    public string? ClosedCycleAuthorizedByUserId { get; private set; }
    public DateTimeOffset? ClosedCycleAuthorizedAt { get; private set; }
    public Guid? ReversalOfOperationalTransactionId { get; private set; }
    public string? ReversalReason { get; private set; }
    public string? ReversedByUserId { get; private set; }
    public DateTimeOffset? ReversedAt { get; private set; }
    public string CorrelationId { get; private set; } = string.Empty;
    public IReadOnlyCollection<TransactionAllocation> Allocations => _allocations.AsReadOnly();

    public static OperationalTransaction Create(Guid tenantId, Guid farmId,
        OperationalTransactionType type, OperationalFinanceCategory category, DateOnly eventDate,
        string payeeOrPayer, decimal amountUsd, string? sourceReference, string? notes,
        string createdByUserId, DateTimeOffset createdAt, string correlationId) =>
        new(tenantId, farmId, type, category, eventDate, payeeOrPayer, amountUsd,
            sourceReference, notes, createdByUserId, createdAt, correlationId);

    public void Update(OperationalTransactionType type, OperationalFinanceCategory category,
        DateOnly eventDate, string payeeOrPayer, decimal amountUsd, string? sourceReference,
        string? notes, long expectedVersion)
    {
        RequireDraft(expectedVersion);
        ValidateAmount(amountUsd);
        ArgumentException.ThrowIfNullOrWhiteSpace(payeeOrPayer);
        Type = type;
        Category = category;
        EventDate = eventDate;
        PayeeOrPayer = payeeOrPayer.Trim();
        AmountUsd = amountUsd;
        SourceReference = Clean(sourceReference);
        Notes = Clean(notes);
        Version++;
    }

    public void ReplaceAllocations(IReadOnlyCollection<TransactionAllocation> allocations,
        long expectedVersion)
    {
        RequireDraft(expectedVersion);
        if (allocations.Any(x => x.OperationalTransactionId != Id || x.TenantId != TenantId || x.FarmId != FarmId))
            throw new InvalidOperationException("Every allocation must belong to this transaction, tenant, and farm.");
        if (allocations.Sum(x => x.AmountUsd) != AmountUsd)
            throw new InvalidOperationException("Allocations must reconcile exactly to the transaction amount.");
        _allocations.Clear();
        _allocations.AddRange(allocations);
        Version++;
    }

    public void Post(string userId, DateTimeOffset at, string idempotencyKey, long expectedVersion,
        bool isClosedCycleCorrection = false, string? correctionReason = null)
    {
        RequireDraft(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        if (_allocations.Count == 0 || _allocations.Sum(x => x.AmountUsd) != AmountUsd)
            throw new InvalidOperationException("Allocations must reconcile exactly before posting.");
        if (isClosedCycleCorrection && string.IsNullOrWhiteSpace(correctionReason))
            throw new InvalidOperationException("A closed-cycle correction requires a reason.");
        Status = OperationalTransactionStatus.Posted;
        PostedByUserId = userId.Trim();
        PostedAt = at;
        PostedIdempotencyKey = idempotencyKey.Trim();
        IsClosedCycleCorrection = isClosedCycleCorrection;
        ClosedCycleCorrectionReason = isClosedCycleCorrection ? correctionReason!.Trim() : null;
        ClosedCycleAuthorizedByUserId = isClosedCycleCorrection ? userId.Trim() : null;
        ClosedCycleAuthorizedAt = isClosedCycleCorrection ? at : null;
        Version++;
    }

    public void Cancel(long expectedVersion)
    {
        RequireDraft(expectedVersion);
        Status = OperationalTransactionStatus.Cancelled;
        Version++;
    }

    public static OperationalTransaction CreateReversal(OperationalTransaction original,
        string reason, string userId, DateTimeOffset at, string idempotencyKey, string correlationId)
    {
        if (original.Status != OperationalTransactionStatus.Posted)
            throw new InvalidOperationException("Only a posted transaction can be reversed.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        ArgumentException.ThrowIfNullOrWhiteSpace(idempotencyKey);
        var reversal = new OperationalTransaction(original.TenantId, original.FarmId, original.Type,
            original.Category, original.EventDate, original.PayeeOrPayer, original.AmountUsd,
            original.SourceReference, original.Notes, userId, at, correlationId)
        {
            Status = OperationalTransactionStatus.Reversed,
            ReversalOfOperationalTransactionId = original.Id,
            ReversalReason = reason.Trim(),
            ReversedByUserId = userId.Trim(),
            ReversedAt = at,
            PostedByUserId = userId.Trim(),
            PostedAt = at,
            PostedIdempotencyKey = idempotencyKey.Trim(),
            IsClosedCycleCorrection = false,
            Version = 1
        };
        foreach (var allocation in original.Allocations)
            reversal._allocations.Add(TransactionAllocation.Create(original.TenantId,
                original.FarmId, reversal.Id, allocation.CropCycleId, allocation.FieldId,
                allocation.Category, allocation.AmountUsd, allocation.AllocationType, at));
        return reversal;
    }

    private void RequireDraft(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("This transaction changed after it was loaded. Refresh and try again.");
        if (Status != OperationalTransactionStatus.Draft)
            throw new InvalidOperationException("Posted, reversed, or cancelled transactions are immutable.");
    }

    private static void ValidateAmount(decimal amountUsd)
    {
        if (amountUsd <= 0) throw new ArgumentOutOfRangeException(nameof(amountUsd), "Amount must be positive.");
        if (decimal.Round(amountUsd, 2, MidpointRounding.AwayFromZero) != amountUsd)
            throw new ArgumentException("USD amounts support at most two decimal places.", nameof(amountUsd));
    }

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
