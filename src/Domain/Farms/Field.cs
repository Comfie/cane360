namespace Cane360.Domain.Farms;

public sealed class Field : BaseAuditableEntity
{
    private readonly List<CropCycle> _cropCycles = [];

    private Field() { }

    private Field(
        Guid farmId,
        string code,
        string name,
        decimal declaredHectares,
        decimal? mappedHectares,
        ReportingAreaSource reportingAreaSource,
        string irrigationMethod,
        string? soilNotes)
    {
        FarmId = farmId;
        Code = code;
        Name = name.Trim();
        DeclaredHectares = declaredHectares;
        MappedHectares = mappedHectares;
        ReportingAreaSource = reportingAreaSource;
        IrrigationMethod = irrigationMethod.Trim();
        SoilNotes = string.IsNullOrWhiteSpace(soilNotes) ? null : soilNotes.Trim();
        Status = RecordStatus.Active;
    }

    public Guid FarmId { get; private set; }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public decimal DeclaredHectares { get; private set; }
    public decimal? MappedHectares { get; private set; }
    public ReportingAreaSource ReportingAreaSource { get; private set; }
    public string IrrigationMethod { get; private set; } = string.Empty;
    public string? SoilNotes { get; private set; }
    public RecordStatus Status { get; private set; }
    public IReadOnlyCollection<CropCycle> CropCycles => _cropCycles.AsReadOnly();

    public decimal ReportingHectares => ReportingAreaSource switch
    {
        ReportingAreaSource.Declared => DeclaredHectares,
        ReportingAreaSource.Mapped when MappedHectares is > 0 => MappedHectares.Value,
        _ => throw new InvalidOperationException("Mapped hectares are required when mapped area is selected for reporting.")
    };

    public CropCycle? CurrentCropCycle => _cropCycles.SingleOrDefault(cycle =>
        cycle.Status is CropCycleStatus.Active or CropCycleStatus.ReadyForHarvest);

    internal static Field Create(
        Guid farmId,
        string code,
        string name,
        decimal declaredHectares,
        decimal? mappedHectares,
        ReportingAreaSource reportingAreaSource,
        string irrigationMethod,
        string? soilNotes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentException.ThrowIfNullOrWhiteSpace(irrigationMethod);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(declaredHectares);

        if (mappedHectares is <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(mappedHectares), "Mapped hectares must be greater than zero.");
        }

        if (reportingAreaSource == ReportingAreaSource.Mapped && mappedHectares is null)
        {
            throw new InvalidOperationException("Mapped hectares are required when mapped area is selected for reporting.");
        }

        return new Field(
            farmId,
            code,
            name,
            declaredHectares,
            mappedHectares,
            reportingAreaSource,
            irrigationMethod,
            soilNotes);
    }

    public CropCycle OpenCurrentCropCycle(
        CropCycleType cycleType,
        int? ratoonNumber,
        string variety,
        DateOnly startDate,
        DateOnly expectedHarvestStart,
        DateOnly expectedHarvestEnd,
        decimal expectedYieldTonnes)
    {
        if (CurrentCropCycle is not null)
        {
            throw new InvalidOperationException("This field already has a current crop cycle.");
        }

        var cropCycle = CropCycle.Open(
            Id,
            cycleType,
            ratoonNumber,
            variety,
            startDate,
            expectedHarvestStart,
            expectedHarvestEnd,
            expectedYieldTonnes);
        _cropCycles.Add(cropCycle);

        return cropCycle;
    }
}
