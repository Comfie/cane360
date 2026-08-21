using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class GetActivityDetailsQueryHandler(
    IFarmSetupRepository repository,
    ILabourRepository labourRepository,
    IUser user,
    IIdentityService identityService) : IRequestHandler<GetActivityDetailsQuery, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(GetActivityDetailsQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var records = await labourRepository.GetWorkRecordsAsync(
            tenant.Id, farm.Id, null, null, request.ActivityId, false, cancellationToken);
        var workers = await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        return await ActivityMapper.MapDetailsAsync(
            tenant, ActivityAccess.RequireActivity(tenant, request.ActivityId), identityService, records, workers);
    }
}
