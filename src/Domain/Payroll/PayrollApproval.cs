namespace Cane360.Domain.Payroll;

public sealed class PayrollApproval : BaseEntity
{
    private PayrollApproval() { }
    private PayrollApproval(Guid runId, Guid calculationId, Guid tenantId, Guid farmId, long runVersion, int calculationVersion, bool approved, string? reason, DateTimeOffset at, string userId, Guid? personId, string key)
    { PayrollRunId = runId; PayrollCalculationId = calculationId; TenantId = tenantId; FarmId = farmId; RunVersion = runVersion; CalculationVersion = calculationVersion; Approved = approved; Reason = reason?.Trim(); DecidedAt = at; DecidedByUserId = userId.Trim(); DecidedByPersonId = personId; IdempotencyKey = key.Trim(); }
    public Guid PayrollRunId { get; private set; } public Guid PayrollCalculationId { get; private set; } public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public long RunVersion { get; private set; } public int CalculationVersion { get; private set; } public bool Approved { get; private set; } public string? Reason { get; private set; } public DateTimeOffset DecidedAt { get; private set; } public string DecidedByUserId { get; private set; } = string.Empty; public Guid? DecidedByPersonId { get; private set; } public string IdempotencyKey { get; private set; } = string.Empty;
    public static PayrollApproval Create(Guid runId, Guid calculationId, Guid tenantId, Guid farmId, long runVersion, int calculationVersion, bool approved, string? reason, DateTimeOffset at, string userId, Guid? personId, string key)
    { if (!approved && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A rejection reason is required."); ArgumentException.ThrowIfNullOrWhiteSpace(key); return new(runId, calculationId, tenantId, farmId, runVersion, calculationVersion, approved, reason, at, userId, personId, key); }
}
