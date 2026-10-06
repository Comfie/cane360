namespace Cane360.Application.Labour;

public sealed record UpdateWorkerProfileCommand(Guid WorkerId, long ExpectedVersion, long ExpectedPersonVersion,
    string DisplayName, string? Phone, string EmploymentType, WorkerProfileInput Profile) : IRequest<WorkerDetailsDto>;
