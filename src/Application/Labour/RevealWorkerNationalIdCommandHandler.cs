using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class RevealWorkerNationalIdCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IWorkerSensitiveDataProtector protector,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<RevealWorkerNationalIdCommand, RevealedNationalIdDto>
{
    public async Task<RevealedNationalIdDto> Handle(RevealWorkerNationalIdCommand request,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = LabourAccess.RequireFarm(tenant);
        string userId = LabourAccess.RequireUserId(user);
        string securityRole = LabourAccess.SecurityRole(tenant, userId);
        if (securityRole != TenantSecurityRoles.Grower)
        {
            throw new ForbiddenAccessException();
        }

        WorkerProfile worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken),
            request.WorkerId);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "NationalIdRevealRequested", userId, securityRole, worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Authorised full national-ID reveal requested."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        string nationalId = protector.Reveal(tenant.Id, farm.Id, worker.Id,
            worker.NationalIdCiphertext, worker.NationalIdNonce, worker.NationalIdTag, worker.NationalIdKeyId);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "NationalIdRevealSucceeded", userId, securityRole, worker.PersonId,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), null,
            "Authorised full national-ID reveal succeeded."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return new RevealedNationalIdDto(worker.Id, nationalId);
    }
}
