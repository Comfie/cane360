namespace Cane360.Domain.Payroll;

public sealed class PayrollRun : BaseEntity
{
    private PayrollRun() { }

    private PayrollRun(Guid tenantId, Guid farmId, Guid periodId, DateTimeOffset at, string userId, Guid? personId)
    {
        TenantId = tenantId; FarmId = farmId; PayrollPeriodId = periodId; Status = PayrollRunStatus.Draft;
        CreatedAt = at; CreatedByUserId = userId.Trim(); CreatedByPersonId = personId;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid PayrollPeriodId { get; private set; }
    public PayrollRunStatus Status { get; private set; }
    public int LatestCalculationVersion { get; private set; }
    public int? SubmittedCalculationVersion { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public Guid? CreatedByPersonId { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; }
    public string? SubmittedByUserId { get; private set; }
    public DateTimeOffset? ApprovedAt { get; private set; }
    public DateTimeOffset? RejectedAt { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancellationReason { get; private set; }
    public long Version { get; private set; }

    public static PayrollRun Create(Guid tenantId, Guid farmId, Guid periodId, DateTimeOffset at, string userId, Guid? personId)
    {
        if (tenantId == Guid.Empty || farmId == Guid.Empty || periodId == Guid.Empty) throw new ArgumentException("Tenant, farm, and payroll period are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        return new(tenantId, farmId, periodId, at, userId, personId);
    }

    public int RecordCalculation(long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is PayrollRunStatus.Approved or PayrollRunStatus.Cancelled or PayrollRunStatus.PendingGrowerApproval) throw new InvalidOperationException("This payroll run cannot be recalculated in its current state.");
        LatestCalculationVersion++;
        SubmittedCalculationVersion = null;
        SubmittedAt = null;
        SubmittedByUserId = null;
        Status = PayrollRunStatus.Calculated;
        Version++;
        return LatestCalculationVersion;
    }

    public void Submit(int calculationVersion, DateTimeOffset at, string userId, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != PayrollRunStatus.Calculated || calculationVersion != LatestCalculationVersion) throw new InvalidOperationException("Only the latest calculated version can be submitted.");
        Status = PayrollRunStatus.PendingGrowerApproval; SubmittedCalculationVersion = calculationVersion; SubmittedAt = at; SubmittedByUserId = userId.Trim(); Version++;
    }

    public void Decide(bool approved, int calculationVersion, DateTimeOffset at, string? reason, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != PayrollRunStatus.PendingGrowerApproval || SubmittedCalculationVersion != calculationVersion) throw new InvalidOperationException("The exact submitted payroll calculation version is required.");
        if (!approved && string.IsNullOrWhiteSpace(reason)) throw new ArgumentException("A rejection reason is required.");
        Status = approved ? PayrollRunStatus.Approved : PayrollRunStatus.Rejected;
        if (approved) ApprovedAt = at; else { RejectedAt = at; RejectionReason = reason!.Trim(); }
        Version++;
    }

    public void Cancel(DateTimeOffset at, string reason, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status is not (PayrollRunStatus.Draft or PayrollRunStatus.Calculated or PayrollRunStatus.Rejected)) throw new InvalidOperationException("Only a draft, calculated, or rejected payroll run can be cancelled.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = PayrollRunStatus.Cancelled; CancelledAt = at; CancellationReason = reason.Trim(); Version++;
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This payroll run changed after it was loaded. Refresh and try again.");
    }
}
