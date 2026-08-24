using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class RevealWorkerNationalIdCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IWorkerSensitiveDataProtector protector,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<RevealWorkerNationalIdCommand, RevealedNationalIdDto>
{
    public async Task<RevealedNationalIdDto> Handle(RevealWorkerNationalIdCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken), request.WorkerId);
        var userId = LabourAccess.RequireUserId(user);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "NationalIdRevealRequested", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Authorised full national-ID reveal requested."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        var nationalId = protector.Reveal(tenant.Id, farm.Id, worker.Id,
            worker.NationalIdCiphertext, worker.NationalIdNonce, worker.NationalIdTag, worker.NationalIdKeyId);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "NationalIdRevealSucceeded", userId, LabourAccess.SecurityRole(tenant, userId), worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Authorised full national-ID reveal succeeded."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return new RevealedNationalIdDto(worker.Id, nationalId);
    }
}
