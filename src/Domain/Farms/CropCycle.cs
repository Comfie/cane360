namespace Cane360.Domain.Farms;

public sealed class CropCycle : BaseAuditableEntity
{
    private CropCycle() { }

    private CropCycle(
        Guid fieldId,
        CropCycleType cycleType,
        int? ratoonNumber,
        string variety,
        DateOnly startDate,
        DateOnly expectedHarvestStart,
        DateOnly expectedHarvestEnd,
        decimal expectedYieldTonnes)
    {
        FieldId = fieldId;
        CycleType = cycleType;
        RatoonNumber = ratoonNumber;
        Variety = variety.Trim();
        StartDate = startDate;
        ExpectedHarvestStart = expectedHarvestStart;
        ExpectedHarvestEnd = expectedHarvestEnd;
        ExpectedYieldTonnes = expectedYieldTonnes;
        Status = CropCycleStatus.Active;
    }

    public Guid FieldId { get; private set; }
    public CropCycleType CycleType { get; private set; }
    public int? RatoonNumber { get; private set; }
    public string Variety { get; private set; } = string.Empty;
    public DateOnly StartDate { get; private set; }
    public DateOnly ExpectedHarvestStart { get; private set; }
    public DateOnly ExpectedHarvestEnd { get; private set; }
    public decimal ExpectedYieldTonnes { get; private set; }
    public CropCycleStatus Status { get; private set; }

    internal static CropCycle Open(
        Guid fieldId,
        CropCycleType cycleType,
        int? ratoonNumber,
        string variety,
        DateOnly startDate,
        DateOnly expectedHarvestStart,
        DateOnly expectedHarvestEnd,
        decimal expectedYieldTonnes)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(variety);
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
            variety,
            startDate,
            expectedHarvestStart,
            expectedHarvestEnd,
            expectedYieldTonnes);
    }
}
