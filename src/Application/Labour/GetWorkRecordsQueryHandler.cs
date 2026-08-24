using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class GetWorkRecordsQueryHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user)
    : IRequestHandler<GetWorkRecordsQuery, IReadOnlyList<WorkRecordDto>>
{
    public async Task<IReadOnlyList<WorkRecordDto>> Handle(GetWorkRecordsQuery request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        if (request.WorkerId.HasValue)
        {
            LabourAccess.RequireWorker(await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId.Value, false, cancellationToken), request.WorkerId.Value);
        }
        if (request.ActivityId.HasValue) LabourAccess.RequireActivity(tenant, request.ActivityId.Value);
        var workers = (await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(worker => worker.Id);
        var records = await labourRepository.GetWorkRecordsAsync(tenant.Id, farm.Id, request.WorkDate, request.WorkerId, request.ActivityId, false, cancellationToken);
        return records.Select(record => LabourMapper.Work(tenant, farm, workers, record)).ToArray();
    }
}
