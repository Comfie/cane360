using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class GetActivitiesQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetActivitiesQuery, ActivityCollectionDto>
{
    public async Task<ActivityCollectionDto> Handle(GetActivitiesQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        IEnumerable<Activity> query = farm.Fields.SelectMany(field => field.CropCycles).SelectMany(cycle => cycle.Activities);
        if (request.FieldId.HasValue) query = query.Where(activity => activity.FieldId == request.FieldId);
        if (request.CropCycleId.HasValue) query = query.Where(activity => activity.CropCycleId == request.CropCycleId);
        if (request.ActivityTypeId.HasValue) query = query.Where(activity => activity.ActivityTypeId == request.ActivityTypeId);
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = Enum.Parse<ActivityStatus>(request.Status, true);
            query = query.Where(activity => activity.Status == status);
        }
        if (request.FromDate.HasValue) query = query.Where(activity => OperationalDate(activity) >= request.FromDate);
        if (request.ToDate.HasValue) query = query.Where(activity => OperationalDate(activity) <= request.ToDate);

        var ordered = query.OrderByDescending(OperationalDate).ThenByDescending(activity => activity.Created).ToArray();
        var items = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(activity => ActivityMapper.MapListItem(farm, activity)).ToArray();
        return new ActivityCollectionDto(
            items,
            request.Page,
            request.PageSize,
            ordered.Length,
            (int)Math.Ceiling(ordered.Length / (double)request.PageSize));
    }

    private static DateOnly OperationalDate(Activity activity) => activity.ActualAt.HasValue
        ? ActivityAccess.HarareDate(activity.ActualAt.Value)
        : activity.PlannedDate ?? DateOnly.FromDateTime(activity.Created.UtcDateTime);
}
