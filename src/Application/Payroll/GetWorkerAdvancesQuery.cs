namespace Cane360.Application.Payroll;

public sealed record GetWorkerAdvancesQuery : IRequest<IReadOnlyList<WorkerAdvanceDto>>;
