using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class EndWorkerRateCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<EndWorkerRateCommand, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(EndWorkerRateCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken), request.WorkerId);
        var rates = await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, true, cancellationToken);
        var rate = rates.SingleOrDefault(candidate => candidate.Id == request.RateId)
            ?? throw new NotFoundException(request.RateId.ToString(), "Worker rate");
        LabourAccess.ApplyDomainAction(nameof(request.EffectiveTo), () => rate.End(request.EffectiveTo, request.ExpectedVersion));
        var userId = LabourAccess.RequireUserId(user);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerRate), rate.Id,
            "RateEnded", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Worker rate effective period ended."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return new WorkerDetailsDto(LabourMapper.Worker(farm, worker), rates.Select(item => LabourMapper.Rate(tenant, item)).ToArray());
    }
}
