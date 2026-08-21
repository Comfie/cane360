using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed record GetActivitiesQuery(
    Guid? FieldId,
    Guid? CropCycleId,
    Guid? ActivityTypeId,
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 25) : IRequest<ActivityCollectionDto>;
