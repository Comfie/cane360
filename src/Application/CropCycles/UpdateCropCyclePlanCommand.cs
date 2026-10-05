namespace Cane360.Application.CropCycles;

public sealed record UpdateCropCyclePlanCommand(Guid FieldId, Guid CropCycleId, long ExpectedVersion,
    DateOnly StartDate, DateOnly? ExpectedHarvestStart, DateOnly? ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes) : IRequest<CropCycleDetailsDto>;
