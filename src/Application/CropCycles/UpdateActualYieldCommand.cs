namespace Cane360.Application.CropCycles;

public sealed record UpdateActualYieldCommand(Guid FieldId, Guid CropCycleId, long ExpectedVersion,
    decimal ActualTonnes) : IRequest<CropCycleDetailsDto>;
