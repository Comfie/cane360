namespace Cane360.Domain.Farms;

public sealed class HarvestResult : BaseAuditableEntity
{
    private HarvestResult() { }

    private HarvestResult(Guid cropCycleId, DateOnly harvestDate, decimal actualTonnes)
    {
        CropCycleId = cropCycleId;
        HarvestDate = harvestDate;
        ActualTonnes = actualTonnes;
    }

    public Guid CropCycleId { get; private set; }
    public DateOnly HarvestDate { get; private set; }
    public decimal ActualTonnes { get; private set; }

    internal static HarvestResult Create(Guid cropCycleId, DateOnly harvestDate, decimal actualTonnes)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(actualTonnes);
        return new HarvestResult(cropCycleId, harvestDate, actualTonnes);
    }
}
