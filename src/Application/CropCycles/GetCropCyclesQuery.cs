namespace Cane360.Application.CropCycles;

public sealed record GetCropCyclesQuery(Guid FieldId) : IRequest<CropCycleCollectionDto>;
