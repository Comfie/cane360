namespace Cane360.Application.Activities;

public sealed class GetActivityTypesQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetActivityTypesQuery, IReadOnlyList<ActivityTypeDto>>
{
    public async Task<IReadOnlyList<ActivityTypeDto>> Handle(GetActivityTypesQuery request,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireCaptureTenantAsync(repository, user, false, cancellationToken);
        return tenant.ActivityTypes.OrderBy(type => type.Name).Select(Map).ToArray();
    }

    internal static ActivityTypeDto Map(ActivityType type)
    {
        return new ActivityTypeDto(
            type.Id, type.Code, type.Name, type.SupportsPlanned, type.SupportsUnplanned,
            type.QuantityBasis.ToString(), type.Status.ToString(), type.Version);
    }
}
