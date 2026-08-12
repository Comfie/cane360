namespace Cane360.Domain.Activities;

public sealed class ActivityStatusChange : BaseEntity
{
    private ActivityStatusChange() { }

    private ActivityStatusChange(
        Guid activityId,
        Guid tenantId,
        Guid farmId,
        ActivityStatus fromStatus,
        ActivityStatus toStatus,
        DateTimeOffset recordedAt,
        string recordedBy,
        Guid? operationalPersonId,
        string? reason)
    {
        ActivityId = activityId;
        TenantId = tenantId;
        FarmId = farmId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        RecordedAt = recordedAt;
        RecordedBy = recordedBy.Trim();
        OperationalPersonId = operationalPersonId;
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public Guid ActivityId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public ActivityStatus FromStatus { get; private set; }
    public ActivityStatus ToStatus { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public string RecordedBy { get; private set; } = string.Empty;
    public Guid? OperationalPersonId { get; private set; }
    public string? Reason { get; private set; }

    internal static ActivityStatusChange Create(
        Guid activityId,
        Guid tenantId,
        Guid farmId,
        ActivityStatus fromStatus,
        ActivityStatus toStatus,
        DateTimeOffset recordedAt,
        string recordedBy,
        Guid? operationalPersonId = null,
        string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);
        return new ActivityStatusChange(
            activityId, tenantId, farmId, fromStatus, toStatus, recordedAt, recordedBy, operationalPersonId, reason);
    }
}
