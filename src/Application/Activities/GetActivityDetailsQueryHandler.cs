using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

public sealed class GetActivityDetailsQueryHandler(
    IFarmSetupRepository repository,
    ILabourRepository labourRepository,
    IUser user,
    IIdentityService identityService) : IRequestHandler<GetActivityDetailsQuery, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(GetActivityDetailsQuery request, CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireCaptureTenantAsync(repository, user, false, cancellationToken);
        Farm farm = ActivityAccess.RequireFarm(tenant);
        Activity activity = ActivityAccess.RequireAssignedActivity(tenant, user, request.ActivityId);
        IReadOnlyList<WorkRecord> records = await labourRepository.GetWorkRecordsAsync(
            tenant.Id, farm.Id, null, null, request.ActivityId, false, cancellationToken);
        IReadOnlyList<WorkerProfile> workers =
            await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        return await ActivityMapper.MapDetailsAsync(
            tenant, activity, identityService, records, workers);
    }
}
