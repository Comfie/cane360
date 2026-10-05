namespace Cane360.Application.CropCycles;

public sealed record GetCropVarietiesQuery : IRequest<IReadOnlyList<CropVarietyDto>>;
