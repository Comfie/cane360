namespace Cane360.Application.Labour;

public sealed record ArchiveWorkerCommand(Guid WorkerId, DateOnly ActiveTo, long ExpectedVersion)
    : IRequest<WorkerDetailsDto>;
