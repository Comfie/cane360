namespace Cane360.Application.Payroll;

public sealed record PreflightWorkerTotalDto(Guid WorkerId, string WorkerName, int EligibleCount, int BlockedCount);
