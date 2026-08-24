using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class GetActivityTypesQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetActivityTypesQuery, IReadOnlyList<ActivityTypeDto>>
{
    public async Task<IReadOnlyList<ActivityTypeDto>> Handle(GetActivityTypesQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        return tenant.ActivityTypes.OrderBy(type => type.Name).Select(Map).ToArray();
    }

    internal static ActivityTypeDto Map(ActivityType type) => new(
        type.Id, type.Code, type.Name, type.SupportsPlanned, type.SupportsUnplanned,
        type.QuantityBasis.ToString(), type.Status.ToString(), type.Version);
}
