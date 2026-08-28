namespace Cane360.Application.Payroll;

public sealed record GetWorkerAdvanceQuery(Guid AdvanceId) : IRequest<WorkerAdvanceDto>;
