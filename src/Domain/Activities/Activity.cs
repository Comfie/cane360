using Cane360.Domain.Farms;

namespace Cane360.Domain.Activities;

public sealed class Activity : BaseAuditableEntity
{
    private readonly List<ActivityStatusChange> _statusChanges = [];
    private readonly List<EvidenceLink> _evidenceLinks = [];

    private Activity() { }

    private Activity(
        Guid tenantId,
        Guid farmId,
        Guid fieldId,
        Guid cropCycleId,
        ActivityType activityType,
        ActivityPlanningKind kind,
        DateOnly? plannedDate,
        Guid supervisorPersonId)
    {
        if (!activityType.Supports(kind))
        {
            throw new InvalidOperationException($"This activity type does not support {kind.ToString().ToLowerInvariant()} work.");
        }

        if (kind == ActivityPlanningKind.Planned && plannedDate is null)
        {
            throw new InvalidOperationException("Planned work requires a planned date.");
        }

        TenantId = tenantId;
        FarmId = farmId;
        FieldId = fieldId;
        CropCycleId = cropCycleId;
        ActivityTypeId = activityType.Id;
        ActivityTypeCode = activityType.Code;
        ActivityTypeName = activityType.Name;
        Kind = kind;
        PlannedDate = plannedDate;
        SupervisorPersonId = supervisorPersonId;
        QuantityBasis = activityType.QuantityBasis;
        Status = ActivityStatus.Draft;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid CropCycleId { get; private set; }
    public Guid ActivityTypeId { get; private set; }
    public string ActivityTypeCode { get; private set; } = string.Empty;
    public string ActivityTypeName { get; private set; } = string.Empty;
    public ActivityPlanningKind Kind { get; private set; }
    public DateOnly? PlannedDate { get; private set; }
    public Guid SupervisorPersonId { get; private set; }
    public ActivityQuantityBasis QuantityBasis { get; private set; }
    public DateTimeOffset? ActualAt { get; private set; }
    public decimal? ActualQuantity { get; private set; }
    public Guid? FieldLineProfileId { get; private set; }
    public bool LineContextUnavailable { get; private set; }
    public DateTimeOffset? ActualEnteredAt { get; private set; }
    public string? ActualEnteredByUserId { get; private set; }
    public string? LateEntryReason { get; private set; }
    public int EntryDelayDays { get; private set; }
    public ActivityStatus Status { get; private set; }
    public long Version { get; private set; }
    public IReadOnlyCollection<ActivityStatusChange> StatusChanges => _statusChanges.AsReadOnly();
    public IReadOnlyCollection<EvidenceLink> EvidenceLinks => _evidenceLinks.AsReadOnly();

    public bool IsTerminal => Status is ActivityStatus.Closed or ActivityStatus.Cancelled;
    public bool IsRetrospective => EntryDelayDays > 0;

    internal static Activity Create(
        Guid tenantId,
        Guid farmId,
        Guid fieldId,
        Guid cropCycleId,
        ActivityType activityType,
        ActivityPlanningKind kind,
        DateOnly? plannedDate,
        Guid supervisorPersonId) =>
        new(tenantId, farmId, fieldId, cropCycleId, activityType, kind, plannedDate, supervisorPersonId);

    public void RecordActualWork(
        DateTimeOffset actualAt,
        decimal? actualQuantity,
        decimal fieldReportingHectares,
        FieldLineProfile? lineProfile,
        DateOnly cropCycleStart,
        DateTimeOffset enteredAt,
        string enteredByUserId,
        string? lateEntryReason,
        long expectedVersion)
    {
        RequireVersion(expectedVersion);
        EnsureActualEditable();
        ArgumentException.ThrowIfNullOrWhiteSpace(enteredByUserId);

        if (actualAt > enteredAt)
        {
            throw new InvalidOperationException("Actual work time cannot be in the future.");
        }

        if (HarareDate(actualAt) < cropCycleStart)
        {
            throw new InvalidOperationException("Actual work cannot be before the crop-cycle start date.");
        }

        if (LineContextUnavailable)
        {
            lineProfile = null;
        }

        ValidateQuantity(actualQuantity, fieldReportingHectares, lineProfile);
        var delayDays = CalendarDayDelay(actualAt, enteredAt);
        if (delayDays > 2 && string.IsNullOrWhiteSpace(lateEntryReason))
        {
            throw new InvalidOperationException("A late-entry reason is required when work is entered more than two calendar days later.");
        }

        ActualAt = actualAt;
        ActualQuantity = actualQuantity;
        FieldLineProfileId = QuantityBasis == ActivityQuantityBasis.StandardLines ? lineProfile?.Id : null;
        LineContextUnavailable = QuantityBasis == ActivityQuantityBasis.StandardLines && lineProfile is null;
        ActualEnteredAt = enteredAt;
        ActualEnteredByUserId = enteredByUserId.Trim();
        LateEntryReason = string.IsNullOrWhiteSpace(lateEntryReason) ? null : lateEntryReason.Trim();
        EntryDelayDays = delayDays;
        Version++;
    }

    public ActivityStatusChange Transition(
        ActivityStatus targetStatus,
        DateTimeOffset recordedAt,
        string recordedBy,
        Guid? operationalPersonId,
        string? reason,
        long expectedVersion,
        bool noUnaccountedControlledInput = true,
        bool allRequiredLabourVerified = true)
    {
        RequireVersion(expectedVersion);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);
        var fromStatus = Status;

        if (!IsAllowedTransition(fromStatus, targetStatus))
        {
            throw new InvalidOperationException(
                $"Activity cannot move from {FormatStatus(fromStatus)} to {FormatStatus(targetStatus)}.");
        }

        if (targetStatus == ActivityStatus.Planned)
        {
            if (Kind == ActivityPlanningKind.Planned && PlannedDate is null)
            {
                throw new InvalidOperationException("Planned work requires a planned date.");
            }

            if (Kind == ActivityPlanningKind.Unplanned && ActualAt is null)
            {
                throw new InvalidOperationException("Unplanned work requires actual work details before it can move to Planned.");
            }
        }

        if (targetStatus == ActivityStatus.AwaitingVerification &&
            (ActualAt is null || (QuantityBasis != ActivityQuantityBasis.None && ActualQuantity is null)))
        {
            throw new InvalidOperationException("Actual work and the required quantity must be captured before verification.");
        }

        if (targetStatus == ActivityStatus.ManagerConfirmation && operationalPersonId is null)
        {
            throw new InvalidOperationException("Supervisor verification requires the named supervisor.");
        }

        if (targetStatus == ActivityStatus.Cancelled && string.IsNullOrWhiteSpace(reason))
        {
            throw new InvalidOperationException("A cancellation reason is required.");
        }

        if (targetStatus == ActivityStatus.Closed && !noUnaccountedControlledInput)
        {
            throw new InvalidOperationException("The activity cannot close while controlled inputs remain unaccounted for.");
        }

        if (targetStatus == ActivityStatus.Closed && !allRequiredLabourVerified)
        {
            throw new InvalidOperationException("The activity cannot close while recorded labour remains unverified.");
        }

        Status = targetStatus;
        Version++;
        var change = ActivityStatusChange.Create(
            Id, TenantId, FarmId, fromStatus, targetStatus, recordedAt, recordedBy, operationalPersonId, reason);
        _statusChanges.Add(change);
        return change;
    }

    public EvidenceLink AddSourceReference(
        string reference,
        DateOnly capturedDate,
        DateTimeOffset recordedAt,
        string recordedBy,
        long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (IsTerminal)
        {
            throw new InvalidOperationException("Closed and cancelled activities cannot accept source references.");
        }

        if (capturedDate > HarareDate(recordedAt))
        {
            throw new InvalidOperationException("The source-sheet date cannot be in the future.");
        }

        var link = EvidenceLink.Create(Id, TenantId, FarmId, reference, capturedDate, recordedAt, recordedBy);
        _evidenceLinks.Add(link);
        Version++;
        return link;
    }

    public static bool IsAllowedTransition(ActivityStatus fromStatus, ActivityStatus toStatus) =>
        (fromStatus, toStatus) switch
        {
            (ActivityStatus.Draft, ActivityStatus.Planned or ActivityStatus.Cancelled) => true,
            (ActivityStatus.Planned, ActivityStatus.InProgress or ActivityStatus.Cancelled) => true,
            (ActivityStatus.InProgress, ActivityStatus.AwaitingVerification or ActivityStatus.Cancelled) => true,
            (ActivityStatus.AwaitingVerification, ActivityStatus.ManagerConfirmation or ActivityStatus.InProgress or ActivityStatus.Cancelled) => true,
            (ActivityStatus.ManagerConfirmation, ActivityStatus.Completed or ActivityStatus.InProgress or ActivityStatus.Cancelled) => true,
            (ActivityStatus.Completed, ActivityStatus.Closed) => true,
            _ => false
        };

    private void EnsureActualEditable()
    {
        if (Status is not (ActivityStatus.Draft or ActivityStatus.Planned or ActivityStatus.InProgress))
        {
            throw new InvalidOperationException(
                "Actual work can be changed only while the activity is Draft, Planned, or In progress. Return it to In progress first.");
        }
    }

    private void ValidateQuantity(decimal? quantity, decimal fieldReportingHectares, FieldLineProfile? lineProfile)
    {
        if (QuantityBasis == ActivityQuantityBasis.None)
        {
            if (quantity is not null)
            {
                throw new InvalidOperationException("This activity type does not use a quantity.");
            }

            return;
        }

        if (quantity is null or <= 0)
        {
            throw new InvalidOperationException("A positive actual quantity is required.");
        }

        if (QuantityBasis == ActivityQuantityBasis.Hectares && quantity > fieldReportingHectares)
        {
            throw new InvalidOperationException("Actual hectares cannot exceed the field reporting area.");
        }

        if (QuantityBasis == ActivityQuantityBasis.StandardLines)
        {
            if (decimal.Truncate(quantity.Value) != quantity.Value)
            {
                throw new InvalidOperationException("Standard-line quantity must be a whole number.");
            }

            if (lineProfile is not null && quantity > lineProfile.EstimatedLineCount)
            {
                throw new InvalidOperationException("Actual standard lines cannot exceed the field's estimated whole-line count.");
            }
        }
    }

    private static int CalendarDayDelay(DateTimeOffset actualAt, DateTimeOffset enteredAt)
    {
        var actualDate = HarareDate(actualAt);
        var enteredDate = HarareDate(enteredAt);
        return enteredDate.DayNumber - actualDate.DayNumber;
    }

    private static DateOnly HarareDate(DateTimeOffset value)
    {
        var zone = TimeZoneInfo.FindSystemTimeZoneById("Africa/Harare");
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(value, zone).DateTime);
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion)
        {
            throw new InvalidOperationException("This activity changed after it was loaded. Refresh and try again.");
        }
    }

    private static string FormatStatus(ActivityStatus status) => status switch
    {
        ActivityStatus.InProgress => "In progress",
        ActivityStatus.AwaitingVerification => "Awaiting verification",
        ActivityStatus.ManagerConfirmation => "Manager confirmation",
        _ => status.ToString()
    };
}
