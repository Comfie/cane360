using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

internal static class WorkerNumberRules
{
    public static async Task RequireUniqueAsync(ILabourRepository repository, WorkerProfile worker,
        CancellationToken cancellationToken)
    {
        if (worker.EmployeeNumber is not null && await repository.HasEmployeeNumberAsync(
            worker.TenantId, worker.EmployeeNumber, worker.Id, cancellationToken))
        {
            throw new ConflictException("This employee number is already used in this tenant.");
        }
    }
}
