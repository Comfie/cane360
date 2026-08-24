using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed record CloseCropCycleCommand(
    Guid FieldId,
    Guid CropCycleId,
    long ExpectedVersion) : IRequest<CropCycleDetailsDto>;
