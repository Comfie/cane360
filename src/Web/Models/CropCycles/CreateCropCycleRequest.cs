namespace Cane360.Web.Models.CropCycles;

public sealed record CreateCropCycleRequest(
    string CycleType,
    int? RatoonNumber,
    Guid CropVarietyId,
    DateOnly StartDate,
    DateOnly ExpectedHarvestStart,
    DateOnly ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes);
