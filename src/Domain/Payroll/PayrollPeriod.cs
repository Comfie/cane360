namespace Cane360.Domain.Payroll;

public sealed class PayrollPeriod : BaseAuditableEntity
{
    private PayrollPeriod() { }

    private PayrollPeriod(Guid tenantId, Guid farmId, int year, int month, DateTimeOffset createdAt, string createdByUserId, Guid? createdByPersonId)
    {
        TenantId = tenantId;
        FarmId = farmId;
        Year = year;
        Month = month;
        StartDate = new DateOnly(year, month, 1);
        EndDate = StartDate.AddMonths(1).AddDays(-1);
        DisplayName = StartDate.ToString("MMMM yyyy", System.Globalization.CultureInfo.InvariantCulture);
        CreatedAt = createdAt;
        CreatedByUserId = createdByUserId.Trim();
        CreatedByPersonId = createdByPersonId;
        Status = PayrollPeriodStatus.Draft;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public int Year { get; private set; }
    public int Month { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public PayrollPeriodStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public string CreatedByUserId { get; private set; } = string.Empty;
    public Guid? CreatedByPersonId { get; private set; }
    public DateTimeOffset? OpenedAt { get; private set; }
    public string? OpenedByUserId { get; private set; }
    public Guid? OpenedByPersonId { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    public string? CancelledByUserId { get; private set; }
    public Guid? CancelledByPersonId { get; private set; }
    public string? CancellationReason { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public string? ClosedByUserId { get; private set; }
    public Guid? ClosedByPersonId { get; private set; }
    public Guid? ClosedByPayrollRunId { get; private set; }
    public long Version { get; private set; }

    public static PayrollPeriod Create(Guid tenantId, Guid farmId, int year, int month, DateTimeOffset createdAt, string createdByUserId, Guid? createdByPersonId)
    {
        if (tenantId == Guid.Empty || farmId == Guid.Empty || year is < 2000 or > 9999 || month is < 1 or > 12)
            throw new ArgumentException("A valid tenant, farm, calendar year, and calendar month are required.");
        ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        return new PayrollPeriod(tenantId, farmId, year, month, createdAt, createdByUserId, createdByPersonId);
    }

    public void Open(DateTimeOffset at, string userId, Guid? personId, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != PayrollPeriodStatus.Draft) throw new InvalidOperationException("Only a draft payroll period can be opened.");
        Status = PayrollPeriodStatus.Open;
        OpenedAt = at;
        OpenedByUserId = userId.Trim();
        OpenedByPersonId = personId;
        Version++;
    }

    public void Cancel(DateTimeOffset at, string userId, Guid? personId, string reason, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != PayrollPeriodStatus.Draft) throw new InvalidOperationException("Only a draft payroll period can be cancelled in Phase 6A.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason);
        Status = PayrollPeriodStatus.Cancelled;
        CancelledAt = at;
        CancelledByUserId = userId.Trim();
        CancelledByPersonId = personId;
        CancellationReason = reason.Trim();
        Version++;
    }

    public void Close(DateTimeOffset at, string userId, Guid? personId, Guid payrollRunId, long expectedVersion)
    {
        RequireVersion(expectedVersion);
        if (Status != PayrollPeriodStatus.Open) throw new InvalidOperationException("Only an open payroll period can be closed.");
        if (payrollRunId == Guid.Empty) throw new ArgumentException("A payroll run is required to close the period.");
        ArgumentException.ThrowIfNullOrWhiteSpace(userId);
        Status = PayrollPeriodStatus.Closed;
        ClosedAt = at;
        ClosedByUserId = userId.Trim();
        ClosedByPersonId = personId;
        ClosedByPayrollRunId = payrollRunId;
        Version++;
    }

    private void RequireVersion(long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This payroll period changed after it was loaded. Refresh and try again.");
    }
}
