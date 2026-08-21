using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class GetWorkerDetailsQueryHandler(IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user)
    : IRequestHandler<GetWorkerDetailsQuery, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(GetWorkerDetailsQuery request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken), request.WorkerId);
        var rates = await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        return new WorkerDetailsDto(LabourMapper.Worker(farm, worker), rates.Select(rate => LabourMapper.Rate(tenant, rate)).ToArray());
    }
}
