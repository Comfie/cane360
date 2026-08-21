namespace Cane360.Application.CropCycles;

public sealed record GetCropCycleDetailsQuery(Guid FieldId, Guid CropCycleId) : IRequest<CropCycleDetailsDto>;
