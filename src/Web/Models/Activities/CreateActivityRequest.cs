namespace Cane360.Web.Models.Activities;

public sealed record CreateActivityRequest(
    Guid FieldId,
    Guid CropCycleId,
    Guid ActivityTypeId,
    string Kind,
    DateOnly? PlannedDate,
    Guid SupervisorPersonId);
