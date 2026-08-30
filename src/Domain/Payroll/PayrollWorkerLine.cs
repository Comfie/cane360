namespace Cane360.Domain.Payroll;

public sealed class PayrollWorkerLine : BaseEntity
{
    private readonly List<PayrollEarningLine> _earningLines = [];
    private readonly List<PayrollAdvanceDeduction> _advanceDeductions = [];
    private PayrollWorkerLine() { }
    private PayrollWorkerLine(Guid id, Guid calculationId, Guid tenantId, Guid farmId, Guid workerId, string workerName, IReadOnlyCollection<PayrollEarningLine> earnings, IReadOnlyCollection<PayrollAdvanceDeduction> deductions)
    { Id = id; PayrollCalculationId = calculationId; TenantId = tenantId; FarmId = farmId; WorkerProfileId = workerId; WorkerNameSnapshot = workerName.Trim(); _earningLines.AddRange(earnings); _advanceDeductions.AddRange(deductions); GrossAmountUsd = earnings.Sum(x => x.EarningAmountUsd); DeductionAmountUsd = deductions.Sum(x => x.AmountUsd); NetAmountUsd = GrossAmountUsd - DeductionAmountUsd; if (GrossAmountUsd <= 0 || DeductionAmountUsd < 0 || NetAmountUsd < 0) throw new InvalidOperationException("Payroll worker totals must reconcile and net pay cannot be negative."); }
    public Guid PayrollCalculationId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid WorkerProfileId { get; private set; }
    public string WorkerNameSnapshot { get; private set; } = string.Empty;
    public decimal GrossAmountUsd { get; private set; }
    public decimal DeductionAmountUsd { get; private set; }
    public decimal NetAmountUsd { get; private set; }
    public IReadOnlyCollection<PayrollEarningLine> EarningLines => _earningLines.AsReadOnly();
    public IReadOnlyCollection<PayrollAdvanceDeduction> AdvanceDeductions => _advanceDeductions.AsReadOnly();
    public static PayrollWorkerLine Create(Guid id, Guid calculationId, Guid tenantId, Guid farmId, Guid workerId, string workerName, IReadOnlyCollection<PayrollEarningLine> earnings, IReadOnlyCollection<PayrollAdvanceDeduction> deductions) => new(id, calculationId, tenantId, farmId, workerId, workerName, earnings, deductions);
}
