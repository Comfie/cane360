namespace Cane360.Web.Models.CropCycles;

public sealed record HarvestCropCycleRequest(
    long ExpectedVersion,
    DateOnly HarvestDate,
    decimal ActualTonnes);
