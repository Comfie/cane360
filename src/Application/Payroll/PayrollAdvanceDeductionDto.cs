namespace Cane360.Application.Payroll;

public sealed record PayrollAdvanceDeductionDto(Guid Id, Guid WorkerAdvanceId, Guid AdvanceInstallmentId, Guid RecoveryPayrollPeriodId, int InstallmentSequence, decimal ScheduledAmountUsd, decimal OutstandingBeforeUsd, decimal AmountUsd);
