namespace Cane360.Domain.Labour;

public sealed class WorkRecordActivity : BaseEntity
{
    private WorkRecordActivity() { }

    private WorkRecordActivity(
        Guid workRecordId,
        Guid tenantId,
        Guid farmId,
        Guid fieldId,
        Guid activityId)
    {
        WorkRecordId = workRecordId;
        TenantId = tenantId;
        FarmId = farmId;
        FieldId = fieldId;
        ActivityId = activityId;
    }

    public Guid WorkRecordId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid FieldId { get; private set; }
    public Guid ActivityId { get; private set; }
    public WorkRecord WorkRecord { get; private set; } = null!;

    internal static WorkRecordActivity Create(
        Guid workRecordId,
        Guid tenantId,
        Guid farmId,
        Guid fieldId,
        Guid activityId) =>
        new(workRecordId, tenantId, farmId, fieldId, activityId);
}
