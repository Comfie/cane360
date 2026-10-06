using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class UpdateWorkerProfileCommandHandler(IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository, IUser user, TimeProvider clock)
    : IRequestHandler<UpdateWorkerProfileCommand, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(UpdateWorkerProfileCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, true, cancellationToken);
        Farm farm = LabourAccess.RequireFarm(tenant);
        WorkerProfile worker = LabourAccess.RequireWorker(await labourRepository.GetWorkerAsync(
            tenant.Id, farm.Id, request.WorkerId, true, cancellationToken), request.WorkerId);
        Person person = LabourAccess.RequirePerson(farm, worker.PersonId);
        LabourAccess.ApplyDomainAction(nameof(request.Profile), () =>
        {
            WorkerProfileUpdater.Apply(worker, request.Profile, Enum.Parse<EmploymentType>(request.EmploymentType, true),
                clock, request.ExpectedVersion);
            person.UpdateIdentity(WorkerProfileUpdater.Name(request.Profile, request.DisplayName), request.Phone,
                request.ExpectedPersonVersion);
        });
        await WorkerNumberRules.RequireUniqueAsync(labourRepository, worker, cancellationToken);
        string userId = LabourAccess.RequireUserId(user);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "WorkerProfileUpdated", userId, LabourAccess.SecurityRole(tenant, userId), person.Id,
            clock.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Employee identity, number, employment type and profile details updated."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        IReadOnlyList<WorkerRate> rates = await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        return LabourMapper.Details(farm, worker, rates.Select(rate => LabourMapper.Rate(tenant, rate)).ToArray());
    }
}
