namespace Cane360.Application.CropCycles;

public sealed record CreateCropVarietyCommand(string Code, string Name) : IRequest<CropVarietyDto>;
