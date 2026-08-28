namespace Cane360.Application.Payroll;

public sealed record CancelWorkerAdvanceCommand(Guid AdvanceId, long ExpectedVersion, string Reason) : IRequest<WorkerAdvanceDto>;
