namespace Cane360.Web.Models.CropCycles;

public sealed record CreateCropCycleRequest(
    string CycleType,
    int? RatoonNumber,
    Guid CropVarietyId,
    DateOnly StartDate,
    DateOnly ExpectedHarvestStart,
    DateOnly ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes);

public sealed record TransitionCropCycleRequest(long ExpectedVersion);

public sealed record CancelCropCycleRequest(long ExpectedVersion, string Reason);

public sealed record HarvestCropCycleRequest(
    long ExpectedVersion,
    DateOnly HarvestDate,
    decimal ActualTonnes);

public sealed record CreateCropVarietyRequest(string Code, string Name);
