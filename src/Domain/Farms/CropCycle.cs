using Cane360.Domain.Activities;

namespace Cane360.Domain.Farms;

public sealed class CropCycle : BaseAuditableEntity
{
    private readonly List<Activity> _activities = [];
    private readonly List<CropCycleStatusChange> _statusChanges = [];

    private CropCycle() { }

    private CropCycle(
        Guid fieldId,
        CropCycleType cycleType,
        int? ratoonNumber,
        Guid cropVarietyId,
        string variety,
        DateOnly startDate,
        DateOnly expectedHarvestStart,
        DateOnly expectedHarvestEnd,
        decimal expectedYieldTonnes,
        DateTimeOffset recordedAt,
        string recordedBy)
    {
        FieldId = fieldId;
        CycleType = cycleType;
        RatoonNumber = ratoonNumber;
        CropVarietyId = cropVarietyId;
        Variety = variety.Trim();
        StartDate = startDate;
        ExpectedHarvestStart = expectedHarvestStart;
        ExpectedHarvestEnd = expectedHarvestEnd;
        ExpectedYieldTonnes = expectedYieldTonnes;
        Status = CropCycleStatus.Draft;
        _statusChanges.Add(CropCycleStatusChange.Create(
            Id,
            null,
            CropCycleStatus.Draft,
            recordedAt,
            recordedBy));
    }

    public Guid FieldId { get; }
    public CropCycleType CycleType { get; private set; }
    public int? RatoonNumber { get; private set; }
    public Guid? CropVarietyId { get; private set; }
    public string Variety { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly ExpectedHarvestStart { get; private set; }
    public DateOnly ExpectedHarvestEnd { get; private set; }
    public decimal ExpectedYieldTonnes { get; private set; }
    public CropCycleStatus Status { get; private set; }
    public long Version { get; private set; }
    public HarvestResult? HarvestResult { get; private set; }
    public IReadOnlyCollection<CropCycleStatusChange> StatusChanges => _statusChanges.AsReadOnly();
    public IReadOnlyCollection<Activity> Activities => _activities.AsReadOnly();

    public bool AcceptsOperationalEntries => Status is CropCycleStatus.Active or CropCycleStatus.ReadyForHarvest;

    internal static CropCycle CreateDraft(
        Guid fieldId,
        CropCycleType cycleType,
        int? ratoonNumber,
        Guid cropVarietyId,
        string variety,
        DateOnly startDate,
        DateOnly expectedHarvestStart,
        DateOnly expectedHarvestEnd,
        decimal expectedYieldTonnes,
        DateTimeOffset recordedAt,
        string recordedBy)
    {
        if (cropVarietyId == Guid.Empty)
        {
            throw new ArgumentException("A crop variety is required.", nameof(cropVarietyId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(variety);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedYieldTonnes);

        if (cycleType == CropCycleType.Ratoon && ratoonNumber is null or < 1)
        {
            throw new InvalidOperationException("A ratoon crop cycle requires a ratoon number.");
        }

        if (cycleType == CropCycleType.PlantCane && ratoonNumber is not null)
        {
            throw new InvalidOperationException("Plant cane cannot carry a ratoon number.");
        }

        if (expectedHarvestStart < startDate || expectedHarvestEnd < expectedHarvestStart)
        {
            throw new InvalidOperationException("The expected harvest window must follow the crop-cycle start date.");
        }

        return new CropCycle(
            fieldId,
            cycleType,
            ratoonNumber,
            cropVarietyId,
            variety,
            startDate,
            expectedHarvestStart,
            expectedHarvestEnd,
            expectedYieldTonnes,
            recordedAt,
            recordedBy);
    }

    public int AgeInMonths(DateOnly today)
    {
        DateOnly end = HarvestResult?.HarvestDate ?? today;
        if (end < StartDate)
        {
            return 0;
        }
        int months = (end.Year - StartDate.Year) * 12 + end.Month - StartDate.Month;
        return StartDate.AddMonths(months) > end ? months - 1 : months;
    }

    public void UpdateDraftPlan(DateOnly startDate, DateOnly harvestStart, DateOnly harvestEnd,
        decimal expectedYieldTonnes, DateTimeOffset recordedAt, string recordedBy)
    {
        EnsureStatus(CropCycleStatus.Draft);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(expectedYieldTonnes);
        if (harvestStart < startDate || harvestEnd < harvestStart)
        {
            throw new InvalidOperationException("The expected harvest window must follow the crop-cycle start date.");
        }

        string reason = FormattableString.Invariant(
            $"Plan edited: start {StartDate:yyyy-MM-dd} to {startDate:yyyy-MM-dd}; expected harvest {ExpectedHarvestStart:yyyy-MM-dd}–{ExpectedHarvestEnd:yyyy-MM-dd} to {harvestStart:yyyy-MM-dd}–{harvestEnd:yyyy-MM-dd}; expected tonnes {ExpectedYieldTonnes:F3} to {expectedYieldTonnes:F3}.");
        StartDate = startDate;
        ExpectedHarvestStart = harvestStart;
        ExpectedHarvestEnd = harvestEnd;
        ExpectedYieldTonnes = expectedYieldTonnes;
        RecordEdit(reason, recordedAt, recordedBy);
    }

    public void UpdateActualYield(decimal actualTonnes, DateTimeOffset recordedAt, string recordedBy)
    {
        EnsureStatus(CropCycleStatus.Harvested);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);
        string reason = FormattableString.Invariant(
            $"Manual actual yield edited: {HarvestResult!.ActualTonnes:F3} to {actualTonnes:F3} tonnes.");
        HarvestResult.UpdateActualTonnes(actualTonnes);
        RecordEdit(reason, recordedAt, recordedBy);
    }

    private void RecordEdit(string reason, DateTimeOffset recordedAt, string recordedBy)
    {
        Version++;
        _statusChanges.Add(CropCycleStatusChange.Create(Id, Status, Status, recordedAt, recordedBy, reason));
    }

    internal void Activate(DateTimeOffset recordedAt, string recordedBy)
    {
        TransitionTo(CropCycleStatus.Draft, CropCycleStatus.Active, recordedAt, recordedBy);
    }

    public void MarkReadyForHarvest(DateTimeOffset recordedAt, string recordedBy)
    {
        TransitionTo(CropCycleStatus.Active, CropCycleStatus.ReadyForHarvest, recordedAt, recordedBy);
    }

    public void RecordHarvest(
        DateOnly harvestDate,
        decimal actualTonnes,
        DateOnly today,
        DateTimeOffset recordedAt,
        string recordedBy)
    {
        EnsureStatus(CropCycleStatus.ReadyForHarvest);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);

        if (harvestDate < StartDate)
        {
            throw new InvalidOperationException("The harvest date cannot be before the crop-cycle start date.");
        }

        if (harvestDate > today)
        {
            throw new InvalidOperationException("The harvest date cannot be in the future.");
        }

        if (HarvestResult is not null)
        {
            throw new InvalidOperationException("A harvest result has already been recorded for this crop cycle.");
        }

        if (_activities.Any(activity => activity.Status is not (ActivityStatus.Closed or ActivityStatus.Cancelled)))
        {
            throw new InvalidOperationException(
                "All activities must be Closed or Cancelled before harvest can be recorded.");
        }

        HarvestResult = HarvestResult.Create(Id, harvestDate, actualTonnes);
        TransitionTo(CropCycleStatus.ReadyForHarvest, CropCycleStatus.Harvested, recordedAt, recordedBy);
    }

    public Activity CreateActivity(
        Guid tenantId,
        Guid farmId,
        Guid fieldId,
        ActivityType activityType,
        ActivityPlanningKind kind,
        DateOnly? plannedDate,
        Guid supervisorPersonId)
    {
        if (!AcceptsOperationalEntries)
        {
            throw new InvalidOperationException("Activities require an Active or Ready-for-harvest crop cycle.");
        }

        if (FieldId != fieldId)
        {
            throw new InvalidOperationException("The crop cycle does not belong to the selected field.");
        }

        Activity activity = Activity.Create(
            tenantId, farmId, fieldId, Id, activityType, kind, plannedDate, supervisorPersonId);
        _activities.Add(activity);
        return activity;
    }

    public void Close(DateTimeOffset recordedAt, string recordedBy)
    {
        EnsureStatus(CropCycleStatus.Harvested);
        if (HarvestResult is null)
        {
            throw new InvalidOperationException("A harvest result is required before the crop cycle can be closed.");
        }

        TransitionTo(CropCycleStatus.Harvested, CropCycleStatus.Closed, recordedAt, recordedBy);
    }

    public void Cancel(string reason, DateTimeOffset recordedAt, string recordedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        TransitionTo(CropCycleStatus.Draft, CropCycleStatus.Cancelled, recordedAt, recordedBy, reason);
    }

    private void TransitionTo(
        CropCycleStatus expectedStatus,
        CropCycleStatus nextStatus,
        DateTimeOffset recordedAt,
        string recordedBy,
        string? reason = null)
    {
        EnsureStatus(expectedStatus);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);

        Status = nextStatus;
        Version++;
        _statusChanges.Add(CropCycleStatusChange.Create(
            Id,
            expectedStatus,
            nextStatus,
            recordedAt,
            recordedBy,
            reason));
    }

    private void EnsureStatus(CropCycleStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new InvalidOperationException(
                $"Crop cycle status must be {FormatStatus(expectedStatus)} before this action. Current status is {FormatStatus(Status)}.");
        }
    }

    private static string FormatStatus(CropCycleStatus status)
    {
        return status switch
        {
            CropCycleStatus.ReadyForHarvest => "Ready for harvest",
            _ => status.ToString()
        };
    }
}
