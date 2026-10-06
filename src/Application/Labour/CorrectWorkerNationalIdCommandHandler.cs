using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CorrectWorkerNationalIdCommandHandler(IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository, IWorkerSensitiveDataProtector protector, IUser user, TimeProvider clock)
    : IRequestHandler<CorrectWorkerNationalIdCommand, WorkerDetailsDto>
{
    public async Task<WorkerDetailsDto> Handle(CorrectWorkerNationalIdCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        string userId = LabourAccess.RequireUserId(user);
        string role = LabourAccess.SecurityRole(tenant, userId);
        if (role != TenantSecurityRoles.Grower)
        {
            throw new ForbiddenAccessException();
        }
        Farm farm = LabourAccess.RequireFarm(tenant);
        WorkerProfile worker = LabourAccess.RequireWorker(await labourRepository.GetWorkerAsync(
            tenant.Id, farm.Id, request.WorkerId, true, cancellationToken), request.WorkerId);
        ProtectedNationalId? protectedId = null;
        LabourAccess.ApplyDomainAction(nameof(request.NationalId), () =>
            protectedId = protector.Protect(tenant.Id, farm.Id, worker.Id, request.NationalId));
        if (!worker.NationalIdFingerprint.SequenceEqual(protectedId!.FarmScopedFingerprint) &&
            await labourRepository.HasNationalIdFingerprintAsync(tenant.Id, farm.Id,
                protectedId.FarmScopedFingerprint, cancellationToken))
        {
            throw new ConflictException("A worker with this national ID is already registered on this farm.");
        }
        LabourAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () => worker.CorrectNationalId(
            protectedId.Ciphertext, protectedId.Nonce, protectedId.Tag, protectedId.KeyId,
            protectedId.FarmScopedFingerprint, protectedId.DisplayMask, request.ExpectedVersion));
        // Free-text reasons can contain identifiers. Audit the correction, never the submitted text or secret.
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkerProfile), worker.Id,
            "NationalIdCorrected", userId, role, worker.PersonId, clock.GetUtcNow(), LabourAccess.CorrelationId(user),
            null, "Authorised protected national-ID correction."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        IReadOnlyList<WorkerRate> rates = await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        return LabourMapper.Details(farm, worker, rates.Select(rate => LabourMapper.Rate(tenant, rate)).ToArray());
    }
}
