namespace Cane360.Application.Inventory;

internal sealed record LeakageReportTrace(Guid? FieldId, Guid? CropCycleId, Guid? ActivityId, Guid? IssuerPersonId,
    Guid? RecipientPersonId)
{
    public static LeakageReportTrace Empty { get; } = new(null, null, null, null, null);
}
