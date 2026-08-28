namespace Cane360.Application.Payroll;

public sealed record DecideWorkerAdvanceCommand(Guid AdvanceId, long ExpectedVersion, bool Approved, string? Reason, string IdempotencyKey) : IRequest<WorkerAdvanceDto>;
