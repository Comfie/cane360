namespace Cane360.Application.Payroll;

public sealed record UpdateWorkerAdvanceCommand(Guid AdvanceId, decimal AmountUsd, string Reason, DateOnly RequestedEventDate, Guid RecoveryStartPayrollPeriodId, int InstallmentCount, long ExpectedVersion) : IRequest<WorkerAdvanceDto>;
