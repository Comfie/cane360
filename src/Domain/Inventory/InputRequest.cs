namespace Cane360.Domain.Inventory;

public sealed class InputRequest : BaseAuditableEntity
{
    private readonly List<InputRequestLine> _lines = [];
    private InputRequest() { }

    private InputRequest(Guid tenantId, Guid farmId, Guid fieldId, Guid cropCycleId, Guid activityId,
        DateOnly operationalDate, string requestedByUserId)
    {
        TenantId = tenantId;
        FarmId = farmId;
        FieldId = fieldId;
        CropCycleId = cropCycleId;
        ActivityId = activityId;
        OperationalDate = operationalDate;
        RequestedByUserId = requestedByUserId.Trim();
        Status = InputRequestStatus.Draft;
        Version = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid CropCycleId { get; private set; }
    public Guid ActivityId { get; private set; }
    public DateOnly OperationalDate { get; private set; }
    public string RequestedByUserId { get; private set; } = string.Empty;
    public InputRequestStatus Status { get; private set; }
    public string? RejectionReason { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public string? SubmissionIdempotencyKey { get; private set; }
    public DateTimeOffset? DecidedAt { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<InputRequestLine> Lines => _lines.AsReadOnly();
    public bool RequiresGrower => _lines.Any(line => line.ApprovalRequirement == InputApprovalRequirement.GrowerOnly);

    public static InputRequest Create(Guid tenantId, Guid farmId, Guid fieldId, Guid cropCycleId,
        Guid activityId, DateOnly operationalDate, string requestedByUserId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestedByUserId);
        return new(tenantId, farmId, fieldId, cropCycleId, activityId, operationalDate, requestedByUserId);
    }

    public InputRequestLine AddLine(InventoryItem item, InventoryApplicationRule rule,
        decimal plannedCoverage, decimal requestedQuantity, decimal availableQuantitySnapshot,
        decimal? estimatedUnitCostUsd, long expectedVersion)
    {
        RequireEditable(expectedVersion);
        if (_lines.Any(line => line.InventoryItemId == item.Id))
            throw new InvalidOperationException("An input request may contain an item only once.");
        var line = InputRequestLine.Create(TenantId, FarmId, Id, _lines.Count + 1, item, rule,
            plannedCoverage, requestedQuantity, availableQuantitySnapshot, estimatedUnitCostUsd);
        _lines.Add(line);
        Version++;
        return line;
    }

    public void ChangeLineQuantity(Guid lineId, decimal requestedQuantity, InventoryApplicationRule rule,
        decimal alreadyIssuedQuantity, long expectedVersion)
    {
        RequireEditable(expectedVersion);
        if (alreadyIssuedQuantity > 0) throw new InvalidOperationException("Approved request lines are immutable after the first posted issue.");
        var line = _lines.SingleOrDefault(candidate => candidate.Id == lineId)
            ?? throw new InvalidOperationException("The request line was not found.");
        if (requestedQuantity < alreadyIssuedQuantity)
            throw new InvalidOperationException("Requested quantity cannot be lower than already-issued quantity.");
        line.ChangeRequestedQuantity(requestedQuantity, rule);
        Status = InputRequestStatus.Draft;
        RejectionReason = null;
        Version++;
    }

    public void Submit(DateTimeOffset submittedAt, string idempotencyKey, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != InputRequestStatus.Draft) throw new InvalidOperationException("Only a draft request can be submitted.");
        if (_lines.Count == 0) throw new InvalidOperationException("At least one input line is required.");
        Status = InputRequestStatus.Submitted;
        SubmittedAt = submittedAt;
        SubmissionIdempotencyKey = idempotencyKey.Trim();
        Version++;
    }

    public void RefreshSubmissionSnapshot(Guid lineId, decimal availableQuantity,
        decimal? estimatedUnitCostUsd, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != InputRequestStatus.Draft)
            throw new InvalidOperationException("Submission snapshots can be refreshed only on a draft request.");
        var line = _lines.SingleOrDefault(candidate => candidate.Id == lineId)
            ?? throw new InvalidOperationException("The request line was not found.");
        line.RefreshSubmissionSnapshots(availableQuantity, estimatedUnitCostUsd);
    }

    public void OpenApproval(long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != InputRequestStatus.Submitted) throw new InvalidOperationException("Only a submitted request can enter approval.");
        Status = InputRequestStatus.PendingApproval;
        Version++;
    }

    public void Decide(ApprovalOutcome outcome, string? reason, DateTimeOffset decidedAt, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != InputRequestStatus.PendingApproval) throw new InvalidOperationException("This request is not pending approval.");
        if (outcome == ApprovalOutcome.Rejected && string.IsNullOrWhiteSpace(reason))
            throw new InvalidOperationException("A rejection reason is required.");
        Status = outcome == ApprovalOutcome.Approved ? InputRequestStatus.Approved : InputRequestStatus.Rejected;
        RejectionReason = outcome == ApprovalOutcome.Rejected ? reason!.Trim() : null;
        DecidedAt = decidedAt;
        Version++;
    }

    public void RecordIssued(decimal totalIssued, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is not (InputRequestStatus.Approved or InputRequestStatus.PartiallyIssued))
            throw new InvalidOperationException("Only an approved request can be issued.");
        var totalApproved = _lines.Sum(line => line.RequestedQuantity);
        Status = totalIssued >= totalApproved ? InputRequestStatus.FullyIssued : InputRequestStatus.PartiallyIssued;
        Version++;
    }

    public void RecordIssueReversed(decimal totalIssued, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is not (InputRequestStatus.PartiallyIssued or InputRequestStatus.FullyIssued))
            throw new InvalidOperationException("Only an issued request can record an issue reversal.");
        var totalApproved = _lines.Sum(line => line.RequestedQuantity);
        Status = totalIssued <= 0 ? InputRequestStatus.Approved :
            totalIssued >= totalApproved ? InputRequestStatus.FullyIssued : InputRequestStatus.PartiallyIssued;
        Version++;
    }

    public void Cancel(string reason, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is not (InputRequestStatus.Draft or InputRequestStatus.Submitted))
            throw new InvalidOperationException("Only a draft or submitted request can be cancelled.");
        if (string.IsNullOrWhiteSpace(reason)) throw new InvalidOperationException("A cancellation reason is required.");
        Status = InputRequestStatus.Cancelled;
        CancellationReason = reason.Trim();
        Version++;
    }

    public bool IsDecisionRetry(long subjectVersion) => Version > subjectVersion && DecidedAt.HasValue;
    public bool IsSubmissionRetry(string key) => Status is not InputRequestStatus.Draft && SubmissionIdempotencyKey == key;

    private void RequireEditable(long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is not (InputRequestStatus.Draft or InputRequestStatus.Submitted or InputRequestStatus.PendingApproval or InputRequestStatus.Approved or InputRequestStatus.Rejected))
            throw new InvalidOperationException("This request can no longer be edited.");
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
            throw new InvalidOperationException("This input request changed after it was loaded. Refresh and try again.");
    }
}
