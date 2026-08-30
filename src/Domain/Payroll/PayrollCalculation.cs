namespace Cane360.Domain.Payroll;

public sealed class PayrollCalculation : BaseEntity
{
    private readonly List<PayrollWorkerLine> _workerLines = [];
    private PayrollCalculation() { }
    private PayrollCalculation(Guid id, Guid runId, Guid periodId, Guid tenantId, Guid farmId, int version, decimal gross, decimal deductions, decimal net, int evidenceCount, string blockerSnapshot, string sourceFingerprint, DateTimeOffset at, string userId, Guid? personId)
    { Id = id; PayrollRunId = runId; PayrollPeriodId = periodId; TenantId = tenantId; FarmId = farmId; CalculationVersion = version; GrossAmountUsd = gross; DeductionAmountUsd = deductions; NetAmountUsd = net; EvidenceCount = evidenceCount; BlockerSnapshot = blockerSnapshot; SourceFingerprint = sourceFingerprint; CalculatedAt = at; CalculatedByUserId = userId.Trim(); CalculatedByPersonId = personId; }
    public Guid PayrollRunId { get; private set; }
    public Guid PayrollPeriodId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public int CalculationVersion { get; private set; }
    public decimal GrossAmountUsd { get; private set; }
    public decimal DeductionAmountUsd { get; private set; }
    public decimal NetAmountUsd { get; private set; }
    public int EvidenceCount { get; private set; }
    public string BlockerSnapshot { get; private set; } = "[]";
    public string SourceFingerprint { get; private set; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; private set; }
    public string CalculatedByUserId { get; private set; } = string.Empty;
    public Guid? CalculatedByPersonId { get; private set; }
    public IReadOnlyCollection<PayrollWorkerLine> WorkerLines => _workerLines.AsReadOnly();
    public static PayrollCalculation Create(Guid id, Guid runId, Guid periodId, Guid tenantId, Guid farmId, int version, IReadOnlyCollection<PayrollWorkerLine> workers, IReadOnlyCollection<string> blockers, string fingerprint, DateTimeOffset at, string userId, Guid? personId)
    { var gross = workers.Sum(x => x.GrossAmountUsd); var deductions = workers.Sum(x => x.DeductionAmountUsd); var calculation = new PayrollCalculation(id, runId, periodId, tenantId, farmId, version, gross, deductions, gross - deductions, workers.Sum(x => x.EarningLines.Count), System.Text.Json.JsonSerializer.Serialize(blockers.Order()), fingerprint, at, userId, personId); calculation._workerLines.AddRange(workers); return calculation; }
}
