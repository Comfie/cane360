using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record CreateWorkerRateCommand(
    Guid WorkerId,
    string Basis,
    Guid? ActivityTypeId,
    decimal RateUsd,
    DateOnly EffectiveFrom,
    DateOnly? EffectiveTo) : IRequest<WorkerDetailsDto>;
