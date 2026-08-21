using Cane360.Domain.Farms;

namespace Cane360.Application.CropCycles;

public sealed record CancelCropCycleCommand(
    Guid FieldId,
    Guid CropCycleId,
    long ExpectedVersion,
    string Reason) : IRequest<CropCycleDetailsDto>;
