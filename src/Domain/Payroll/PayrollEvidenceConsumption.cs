namespace Cane360.Domain.Payroll;

public sealed class PayrollEvidenceConsumption : BaseEntity
{
    private PayrollEvidenceConsumption() { }
    private PayrollEvidenceConsumption(Guid runId, Guid calculationId, Guid tenantId, Guid farmId, Guid evidenceId, DateTimeOffset at)
    { PayrollRunId = runId; PayrollCalculationId = calculationId; TenantId = tenantId; FarmId = farmId; EvidenceId = evidenceId; ConsumedAt = at; }
    public Guid PayrollRunId { get; private set; } public Guid PayrollCalculationId { get; private set; } public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid EvidenceId { get; private set; } public DateTimeOffset ConsumedAt { get; private set; }
    public static PayrollEvidenceConsumption Create(Guid runId, Guid calculationId, Guid tenantId, Guid farmId, Guid evidenceId, DateTimeOffset at) => new(runId, calculationId, tenantId, farmId, evidenceId, at);
}
