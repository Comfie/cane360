namespace Cane360.Application.Labour;

public sealed record CreateWorkerCommand(
    Guid? PersonId,
    string? DisplayName,
    string? Phone,
    string EmploymentType,
    DateOnly ActiveFrom,
    string NationalId) : IRequest<WorkerDetailsDto>;
