namespace Cane360.Application.Labour;

public sealed record GetWorkerDetailsQuery(Guid WorkerId) : IRequest<WorkerDetailsDto>;
