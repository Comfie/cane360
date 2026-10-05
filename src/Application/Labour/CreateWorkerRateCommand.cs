namespace Cane360.Application.Labour;

public sealed record CreateWorkerRateCommand(
    Guid WorkerId,
    string Basis,
    Guid? ActivityTypeId,
    decimal RateUsd,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<WorkerDetailsDto>;
