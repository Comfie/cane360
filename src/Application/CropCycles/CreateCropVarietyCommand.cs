using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed record CreateCropVarietyCommand(string Code, string Name) : IRequest<CropVarietyDto>;
