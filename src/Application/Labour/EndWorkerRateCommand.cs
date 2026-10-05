namespace Cane360.Application.Labour;

public sealed record EndWorkerRateCommand(
    Guid WorkerId,
    Guid RateId,
    DateOnly EffectiveTo,
    long ExpectedVersion) : IRequest<WorkerDetailsDto>;
