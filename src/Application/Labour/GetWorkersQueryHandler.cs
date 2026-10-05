using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class GetWorkersQueryHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IUser user)
    : IRequestHandler<GetWorkersQuery, IReadOnlyList<WorkerListItemDto>>
{
    public async Task<IReadOnlyList<WorkerListItemDto>> Handle(GetWorkersQuery request,
        CancellationToken cancellationToken)
    {
        string userId = LabourAccess.RequireUserId(user);
        Tenant tenant = await farmRepository.GetTenantPeopleContextForUserAsync(userId, false,
            cancellationToken) ?? throw new NotFoundException(userId,
            "Active grower or farm-manager membership");
        Farm farm = LabourAccess.RequireFarm(tenant);
        IReadOnlyList<WorkerProfile> workers =
            await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        return workers.Select(worker => LabourMapper.Worker(farm, worker)).OrderBy(worker => worker.DisplayName)
            .ToArray();
    }
}
