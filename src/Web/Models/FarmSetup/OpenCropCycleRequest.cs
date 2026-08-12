namespace Cane360.Web.Models.FarmSetup;

public sealed record OpenCropCycleRequest(
    string CycleType,
    int? RatoonNumber,
    string Variety,
    DateOnly StartDate,
    DateOnly ExpectedHarvestStart,
    DateOnly ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes);
