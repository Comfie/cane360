using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class GetWorkerDetailsQueryHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IUser user)
    : IRequestHandler<GetWorkerDetailsQuery, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(GetWorkerDetailsQuery request, CancellationToken cancellationToken)
    {
        Tenant tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = LabourAccess.RequireFarm(tenant);
        WorkerProfile worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken),
            request.WorkerId);
        IReadOnlyList<WorkerRate> rates =
            await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        return LabourMapper.Details(farm, worker,
            rates.Select(rate => LabourMapper.Rate(tenant, rate)).ToArray());
    }
}
