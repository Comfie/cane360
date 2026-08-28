namespace Cane360.Application.Payroll;

public sealed record CreateWorkerAdvanceCommand(Guid WorkerId, decimal AmountUsd, string Reason, DateOnly RequestedEventDate, Guid RecoveryStartPayrollPeriodId, int? InstallmentCount, IReadOnlyList<Guid> InstallmentPeriodIds) : IRequest<WorkerAdvanceDto>;
