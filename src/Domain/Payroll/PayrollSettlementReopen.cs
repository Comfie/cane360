namespace Cane360.Domain.Payroll;

public sealed class PayrollSettlementReopen : BaseEntity
{
    private PayrollSettlementReopen() { }
    private PayrollSettlementReopen(Guid closureId, Guid tenantId, Guid farmId, Guid runId,
        Guid calculationId, int calculationVersion, string reason, DateTimeOffset at,
        string userId, Guid? personId, string key, string correlationId)
    { PayrollSettlementClosureId = closureId; TenantId = tenantId; FarmId = farmId; PayrollRunId = runId; PayrollCalculationId = calculationId; CalculationVersion = calculationVersion; Reason = reason.Trim(); ReopenedAt = at; ReopenedByUserId = userId.Trim(); ReopenedByPersonId = personId; IdempotencyKey = key.Trim(); CorrelationId = correlationId.Trim(); }
    public Guid PayrollSettlementClosureId { get; private set; } public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; } public Guid PayrollRunId { get; private set; }
    public Guid PayrollCalculationId { get; private set; } public int CalculationVersion { get; private set; }
    public string Reason { get; private set; } = string.Empty; public DateTimeOffset ReopenedAt { get; private set; }
    public string ReopenedByUserId { get; private set; } = string.Empty; public Guid? ReopenedByPersonId { get; private set; }
    public string IdempotencyKey { get; private set; } = string.Empty; public string CorrelationId { get; private set; } = string.Empty;
    public static PayrollSettlementReopen Create(PayrollSettlementClosure closure, string reason,
        DateTimeOffset at, string userId, Guid? personId, string key, string correlationId)
    { ArgumentException.ThrowIfNullOrWhiteSpace(reason); ArgumentException.ThrowIfNullOrWhiteSpace(key); return new(closure.Id, closure.TenantId, closure.FarmId, closure.PayrollRunId, closure.PayrollCalculationId, closure.CalculationVersion, reason, at, userId, personId, key, correlationId); }
}
