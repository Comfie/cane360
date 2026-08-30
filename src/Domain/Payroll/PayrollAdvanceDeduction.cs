namespace Cane360.Domain.Payroll;

public sealed class PayrollAdvanceDeduction : BaseEntity
{
    private PayrollAdvanceDeduction() { }
    private PayrollAdvanceDeduction(Guid workerLineId, Guid calculationId, Guid tenantId, Guid farmId, Guid workerId, Guid advanceId, Guid installmentId, Guid recoveryPeriodId, int sequence, decimal due, decimal outstanding, decimal amount)
    { PayrollWorkerLineId = workerLineId; PayrollCalculationId = calculationId; TenantId = tenantId; FarmId = farmId; WorkerProfileId = workerId; WorkerAdvanceId = advanceId; AdvanceInstallmentId = installmentId; RecoveryPayrollPeriodId = recoveryPeriodId; InstallmentSequence = sequence; ScheduledAmountUsd = due; OutstandingBeforeUsd = outstanding; AmountUsd = amount; }
    public Guid PayrollWorkerLineId { get; private set; } public Guid PayrollCalculationId { get; private set; } public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid WorkerProfileId { get; private set; } public Guid WorkerAdvanceId { get; private set; } public Guid AdvanceInstallmentId { get; private set; } public Guid RecoveryPayrollPeriodId { get; private set; } public int InstallmentSequence { get; private set; } public decimal ScheduledAmountUsd { get; private set; } public decimal OutstandingBeforeUsd { get; private set; } public decimal AmountUsd { get; private set; }
    public static PayrollAdvanceDeduction Create(Guid workerLineId, Guid calculationId, Guid tenantId, Guid farmId, Guid workerId, Guid advanceId, Guid installmentId, Guid periodId, int sequence, decimal scheduled, decimal outstanding, decimal amount)
    { if (amount <= 0 || amount > outstanding || outstanding > scheduled) throw new InvalidOperationException("Advance deduction allocation is invalid."); return new(workerLineId, calculationId, tenantId, farmId, workerId, advanceId, installmentId, periodId, sequence, scheduled, outstanding, amount); }
}
