namespace Cane360.Domain.Activities;

public sealed class EvidenceLink : BaseEntity
{
    private EvidenceLink() { }

    private EvidenceLink(
        Guid activityId,
        Guid tenantId,
        Guid farmId,
        EvidenceRole role,
        string sourceSheetReference,
        DateOnly capturedDate,
        DateTimeOffset recordedAt,
        string recordedBy)
    {
        ActivityId = activityId;
        TenantId = tenantId;
        FarmId = farmId;
        Role = role;
        SourceSheetReference = sourceSheetReference.Trim();
        CapturedDate = capturedDate;
        RecordedAt = recordedAt;
        RecordedBy = recordedBy.Trim();
    }

    public Guid ActivityId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public EvidenceRole Role { get; private set; }
    public string SourceSheetReference { get; private set; } = string.Empty;
    public DateOnly CapturedDate { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public string RecordedBy { get; private set; } = string.Empty;

    internal static EvidenceLink Create(
        Guid activityId,
        Guid tenantId,
        Guid farmId,
        string sourceSheetReference,
        DateOnly capturedDate,
        DateTimeOffset recordedAt,
        string recordedBy)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceSheetReference);
        ArgumentException.ThrowIfNullOrWhiteSpace(recordedBy);
        return new EvidenceLink(
            activityId,
            tenantId,
            farmId,
            EvidenceRole.SourceSheet,
            sourceSheetReference,
            capturedDate,
            recordedAt,
            recordedBy);
    }
}
