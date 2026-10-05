using Cane360.Domain.Labour;

namespace Cane360.Application.CropCycles;

public sealed class GetCropCycleDetailsQueryHandler(
    IFarmSetupRepository repository,
    ILabourRepository labourRepository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<GetCropCycleDetailsQuery, CropCycleDetailsDto>
{
    public async Task<CropCycleDetailsDto> Handle(
        GetCropCycleDetailsQuery request,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await CropCycleAccess.RequireTenantAsync(
            repository, user, false, cancellationToken);
        Field field = CropCycleAccess.RequireField(tenant, request.FieldId);
        CropCycle cropCycle = CropCycleAccess.RequireCycle(field, request.CropCycleId);
        Farm farm = tenant.ActiveFarm!;
        HashSet<Guid> activityIds = cropCycle.Activities.Select(activity => activity.Id).ToHashSet();
        IReadOnlyList<WorkRecord> records =
            await labourRepository.GetWorkRecordsAsync(tenant.Id, farm.Id, null, null, null, false, cancellationToken);
        WorkRecord[] cycleRecords = records
            .Where(record => record.Activities.Any(link => activityIds.Contains(link.ActivityId))).ToArray();
        IReadOnlyList<WorkerProfile> workers =
            await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        return await CropCycleMapper.MapDetailsAsync(field, cropCycle, farm, identityService, cycleRecords, workers,
            DateOnly.FromDateTime(timeProvider.GetUtcNow().UtcDateTime));
    }
}
