using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed record CreateActivityCommand(
    Guid FieldId,
    Guid CropCycleId,
    Guid ActivityTypeId,
    string Kind,
    DateOnly? PlannedDate,
    Guid SupervisorPersonId) : IRequest<ActivityDetailsDto>;
