namespace Cane360.Domain.Payroll;

public sealed class AdvanceInstallment : BaseEntity
{
    private AdvanceInstallment() { }

    private AdvanceInstallment(Guid workerAdvanceId, Guid tenantId, Guid farmId, int sequence, Guid payrollPeriodId, decimal amountUsd)
    {
        WorkerAdvanceId = workerAdvanceId;
        TenantId = tenantId;
        FarmId = farmId;
        Sequence = sequence;
        PayrollPeriodId = payrollPeriodId;
        AmountUsd = amountUsd;
    }

    public Guid WorkerAdvanceId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public int Sequence { get; private set; }
    public Guid PayrollPeriodId { get; private set; }
    public decimal AmountUsd { get; private set; }

    internal static AdvanceInstallment Create(Guid workerAdvanceId, Guid tenantId, Guid farmId, int sequence, Guid payrollPeriodId, decimal amountUsd) =>
        new(workerAdvanceId, tenantId, farmId, sequence, payrollPeriodId, amountUsd);
}
