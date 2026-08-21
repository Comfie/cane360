using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CreateWorkerRateCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<CreateWorkerRateCommand, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(CreateWorkerRateCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken), request.WorkerId);
        if (worker.Status != RecordStatus.Active)
        {
            throw LabourAccess.Failure(nameof(request.WorkerId), "Rates cannot be added to an archived worker.");
        }

        var basis = Enum.Parse<PayBasis>(request.Basis, true);
        if (request.ActivityTypeId.HasValue)
        {
            var activityType = tenant.ActivityTypes.SingleOrDefault(type => type.Id == request.ActivityTypeId.Value)
                ?? throw new NotFoundException(request.ActivityTypeId.Value.ToString(), "Activity type");
            var compatible = (basis, activityType.QuantityBasis) switch
            {
                (PayBasis.Hectare, ActivityQuantityBasis.Hectares) => true,
                (PayBasis.StandardLine, ActivityQuantityBasis.StandardLines) => true,
                _ => false
            };
            if (!compatible)
            {
                throw LabourAccess.Failure(nameof(request.ActivityTypeId), "The activity quantity basis does not match this piece-rate basis.");
            }
        }

        WorkerRate? rate = null;
        LabourAccess.ApplyDomainAction(nameof(request.Basis), () => rate = WorkerRate.Create(
            tenant.Id, farm.Id, worker.Id, basis, request.ActivityTypeId, request.RateUsd,
            request.EffectiveFrom, request.EffectiveTo));
        var rates = await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        if (rates.Any(existing => existing.Overlaps(rate!)))
        {
            throw LabourAccess.Failure(nameof(request.EffectiveFrom), "This rate overlaps another effective rate for the same worker and scope.");
        }

        labourRepository.Add(rate!);
        var userId = LabourAccess.RequireUserId(user);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerRate), rate!.Id,
            "RateCreated", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            $"{basis} USD rate created for an effective date range."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        var updated = rates.Append(rate).OrderByDescending(item => item.EffectiveFrom).Select(item => LabourMapper.Rate(tenant, item)).ToArray();
        return new WorkerDetailsDto(LabourMapper.Worker(farm, worker), updated);
    }
}
