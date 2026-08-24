using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed record CreateCropCycleCommand(
    Guid FieldId,
    string CycleType,
    int? RatoonNumber,
    Guid CropVarietyId,
    DateOnly StartDate,
    DateOnly ExpectedHarvestStart,
    DateOnly ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes) : IRequest<CropCycleDetailsDto>;
