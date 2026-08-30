namespace Cane360.Domain.Payroll;

public sealed class AdvanceRecovery : BaseEntity
{
    private AdvanceRecovery() { }
    private AdvanceRecovery(Guid runId, Guid calculationId, Guid deductionId, Guid tenantId, Guid farmId, Guid advanceId, Guid installmentId, Guid workerId, decimal amount, DateTimeOffset at)
    { PayrollRunId = runId; PayrollCalculationId = calculationId; PayrollAdvanceDeductionId = deductionId; TenantId = tenantId; FarmId = farmId; WorkerAdvanceId = advanceId; AdvanceInstallmentId = installmentId; WorkerProfileId = workerId; AmountUsd = amount; RecoveredAt = at; }
    public Guid PayrollRunId { get; private set; } public Guid PayrollCalculationId { get; private set; } public Guid PayrollAdvanceDeductionId { get; private set; } public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid WorkerAdvanceId { get; private set; } public Guid AdvanceInstallmentId { get; private set; } public Guid WorkerProfileId { get; private set; } public decimal AmountUsd { get; private set; } public DateTimeOffset RecoveredAt { get; private set; }
    public static AdvanceRecovery Create(Guid runId, Guid calculationId, PayrollAdvanceDeduction deduction, DateTimeOffset at) => new(runId, calculationId, deduction.Id, deduction.TenantId, deduction.FarmId, deduction.WorkerAdvanceId, deduction.AdvanceInstallmentId, deduction.WorkerProfileId, deduction.AmountUsd, at);
}
