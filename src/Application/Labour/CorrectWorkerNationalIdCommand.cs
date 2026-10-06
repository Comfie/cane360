namespace Cane360.Application.Labour;

public sealed record CorrectWorkerNationalIdCommand(Guid WorkerId, string NationalId, long ExpectedVersion,
    string Reason) : IRequest<WorkerDetailsDto>;
