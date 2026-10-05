namespace Cane360.Web.Models.CropCycles;

public sealed record UpdateCropCyclePlanRequest(long ExpectedVersion, DateOnly StartDate,
    DateOnly? ExpectedHarvestStart, DateOnly? ExpectedHarvestEnd, decimal ExpectedYieldTonnes);
