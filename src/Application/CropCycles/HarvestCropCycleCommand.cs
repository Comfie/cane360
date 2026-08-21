using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed record HarvestCropCycleCommand(
    Guid FieldId,
    Guid CropCycleId,
    long ExpectedVersion,
    DateOnly HarvestDate,
    decimal ActualTonnes) : IRequest<CropCycleDetailsDto>;
