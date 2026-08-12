namespace Cane360.Domain.Farms;

public sealed class CropCycleStatusChange : BaseEntity
{
    private CropCycleStatusChange() { }

    private CropCycleStatusChange(
        Guid cropCycleId,
        CropCycleStatus? fromStatus,
        CropCycleStatus toStatus,
        DateTimeOffset recordedAt,
        string recordedBy,
        string? reason)
    {
        CropCycleId = cropCycleId;
        FromStatus = fromStatus;
        ToStatus = toStatus;
        RecordedAt = recordedAt;
        RecordedBy = recordedBy.Trim();
        Reason = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
    }

    public Guid CropCycleId { get; private set; }
    public CropCycleStatus? FromStatus { get; private set; }
    public CropCycleStatus ToStatus { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public string RecordedBy { get; private set; } = string.Empty;
    public string? Reason { get; private set; }

    internal static CropCycleStatusChange Create(
        Guid cropCycleId,
        CropCycleStatus? fromStatus,
        CropCycleStatus toStatus,
        DateTimeOffset recordedAt,
        string recordedBy,
        string? reason = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);

        return new CropCycleStatusChange(
            cropCycleId,
            fromStatus,
            toStatus,
            recordedAt,
            recordedBy,
            reason);
    }
}
