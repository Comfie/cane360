namespace Cane360.Application.Payroll;

public sealed record AdvanceRecoveryCandidate(Guid WorkerAdvanceId, Guid AdvanceInstallmentId, Guid RecoveryPayrollPeriodId, int RecoveryYear, int RecoveryMonth, DateTimeOffset AdvanceIssuedAt, int InstallmentSequence, decimal ScheduledAmountUsd, decimal OutstandingAmountUsd);
