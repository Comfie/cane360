using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class ArchiveWorkerCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IUser user,
    TimeProvider timeProvider)
    : IRequestHandler<ArchiveWorkerCommand, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(ArchiveWorkerCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = LabourAccess.RequireFarm(tenant);
        WorkerProfile worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, true, cancellationToken),
            request.WorkerId);
        LabourAccess.ApplyDomainAction(nameof(request.ActiveTo),
            () => worker.Archive(request.ActiveTo, request.ExpectedVersion));
        string userId = LabourAccess.RequireUserId(user);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "WorkerArchived", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Worker archived; historical labour evidence retained."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        IReadOnlyList<WorkerRate> rates =
            await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        return LabourMapper.Details(farm, worker,
            rates.Select(rate => LabourMapper.Rate(tenant, rate)).ToArray());
    }
}
