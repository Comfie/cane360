using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CreateWorkerCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IWorkerSensitiveDataProtector protector,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateWorkerCommand, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(CreateWorkerCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, true, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var userId = LabourAccess.RequireUserId(user);
        var person = request.PersonId.HasValue
            ? LabourAccess.RequirePerson(farm, request.PersonId.Value)
            : farm.AddPerson(request.DisplayName!, request.Phone, request.ActiveFrom);
        if (person.Status != RecordStatus.Active || person.ActiveFrom > request.ActiveFrom || person.ActiveTo < request.ActiveFrom)
        {
            throw LabourAccess.Failure(nameof(request.PersonId), "The selected person must be active on the worker start date.");
        }

        var workerId = Guid.NewGuid();
        ProtectedNationalId? protectedId = null;
        LabourAccess.ApplyDomainAction(nameof(request.NationalId), () =>
            protectedId = protector.Protect(tenant.Id, farm.Id, workerId, request.NationalId));
        if (await labourRepository.HasNationalIdFingerprintAsync(
                tenant.Id, farm.Id, protectedId!.FarmScopedFingerprint, cancellationToken))
        {
            throw new ConflictException("A worker with this national ID is already registered on this farm.");
        }

        WorkerProfile? worker = null;
        LabourAccess.ApplyDomainAction(nameof(request.EmploymentType), () => worker = WorkerProfile.Create(
            workerId, tenant.Id, farm.Id, person.Id, Enum.Parse<EmploymentType>(request.EmploymentType, true),
            request.ActiveFrom, protectedId!.Ciphertext, protectedId.Nonce, protectedId.Tag,
            protectedId.KeyId, protectedId.FarmScopedFingerprint, protectedId.DisplayMask));
        labourRepository.Add(worker!);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker!.Id,
            "WorkerRegistered", userId, LabourAccess.SecurityRole(tenant, userId), person.Id,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Worker registered with protected national-ID evidence."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return new WorkerDetailsDto(LabourMapper.Worker(farm, worker), []);
    }
}
