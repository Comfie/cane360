namespace Cane360.Domain.Payroll;

public sealed class PayrollSettlementClosure : BaseEntity
{
    private PayrollSettlementClosure() { }
    private PayrollSettlementClosure(Guid tenantId, Guid farmId, Guid runId, Guid calculationId,
        int calculationVersion, int sequence, decimal gross, decimal deductions, decimal net,
        decimal activePayments, int workerCount, DateTimeOffset at, string userId, Guid? personId,
        string key, string correlationId)
    { TenantId = tenantId; FarmId = farmId; PayrollRunId = runId; PayrollCalculationId = calculationId; CalculationVersion = calculationVersion; CloseSequence = sequence; GrossAmountUsd = gross; DeductionAmountUsd = deductions; NetAmountUsd = net; ActivePaymentAmountUsd = activePayments; WorkerCount = workerCount; ClosedAt = at; ClosedByUserId = userId.Trim(); ClosedByPersonId = personId; IdempotencyKey = key.Trim(); CorrelationId = correlationId.Trim(); }
    public Guid TenantId { get; private set; } public Guid FarmId { get; private set; }
    public Guid PayrollRunId { get; private set; } public Guid PayrollCalculationId { get; private set; }
    public int CalculationVersion { get; private set; } public int CloseSequence { get; private set; }
    public decimal GrossAmountUsd { get; private set; } public decimal DeductionAmountUsd { get; private set; }
    public decimal NetAmountUsd { get; private set; } public decimal ActivePaymentAmountUsd { get; private set; }
    public int WorkerCount { get; private set; } public DateTimeOffset ClosedAt { get; private set; }
    public string ClosedByUserId { get; private set; } = string.Empty; public Guid? ClosedByPersonId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty; public string CorrelationId { get; private set; } = string.Empty;
    public static PayrollSettlementClosure Create(Guid tenantId, Guid farmId, PayrollRun run,
        PayrollCalculation calculation, int sequence, decimal activePayments, DateTimeOffset at,
        string userId, Guid? personId, string key, string correlationId)
    { if (sequence <= 0 || activePayments != calculation.NetAmountUsd) throw new ArgumentException("Settlement totals must exactly reconcile."); return new(tenantId, farmId, run.Id, calculation.Id, calculation.CalculationVersion, sequence, calculation.GrossAmountUsd, calculation.DeductionAmountUsd, calculation.NetAmountUsd, activePayments, calculation.WorkerLines.Count, at, userId, personId, key, correlationId); }
}
