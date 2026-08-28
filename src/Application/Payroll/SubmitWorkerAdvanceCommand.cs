namespace Cane360.Application.Payroll;

public sealed record SubmitWorkerAdvanceCommand(Guid AdvanceId, long ExpectedVersion) : IRequest<WorkerAdvanceDto>;
