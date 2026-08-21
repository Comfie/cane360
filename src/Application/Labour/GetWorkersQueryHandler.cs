using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class GetWorkersQueryHandler(IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user)
    : IRequestHandler<GetWorkersQuery, IReadOnlyList<WorkerListItemDto>>
{
    public async Task<IReadOnlyList<WorkerListItemDto>> Handle(GetWorkersQuery request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var workers = await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        return workers.Select(worker => LabourMapper.Worker(farm, worker)).OrderBy(worker => worker.DisplayName).ToArray();
    }
}
