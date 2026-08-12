namespace Cane360.Domain.Farms;

public sealed class CropCycle : BaseAuditableEntity
{
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

    public Guid FieldId { get; private set; }
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

    public bool AcceptsOperationalEntries => Status == CropCycleStatus.Active;

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

        HarvestResult = global::Cane360.Domain.Farms.HarvestResult.Create(Id, harvestDate, actualTonnes);
        TransitionTo(CropCycleStatus.ReadyForHarvest, CropCycleStatus.Harvested, recordedAt, recordedBy);
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

    private static string FormatStatus(CropCycleStatus status) => status switch
    {
        CropCycleStatus.ReadyForHarvest => "Ready for harvest",
        _ => status.ToString()
    };
}
