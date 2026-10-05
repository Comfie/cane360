using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CorrectWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IUser user,
    TimeProvider timeProvider)
    : IRequestHandler<CorrectWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(CorrectWorkRecordCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = LabourAccess.RequireFarm(tenant);
        WorkRecord original = LabourAccess.RequireWorkRecord(
            await labourRepository.GetWorkRecordAsync(tenant.Id, farm.Id, request.WorkRecordId, true,
                cancellationToken), request.WorkRecordId);
        string userId = LabourAccess.RequireUserId(user);
        DateTimeOffset now = timeProvider.GetUtcNow();
        LabourAccess.ApplyDomainAction(nameof(request.CorrectionReason), () => original.Supersede(
            request.CorrectionReason, userId, now, request.ExpectedVersion));
        CreateWorkRecordCommand replacementRequest = new(original.WorkerProfileId, original.WorkDate,
            request.PayBasis, request.ActivityIds, request.Quantity, request.Scope, request.LateEntryReason);
        WorkRecord replacement = await WorkRecordActions.BuildAsync(tenant, farm, labourRepository, user, timeProvider,
            replacementRequest, original.Id, cancellationToken);
        labourRepository.Add(replacement);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkRecord), original.Id,
            "EvidenceSuperseded", userId, LabourAccess.SecurityRole(tenant, userId),
            original.Verification?.SupervisorPersonId,
            now, LabourAccess.CorrelationId(user), request.CorrectionReason,
            "Labour evidence superseded by an explicit correction record."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return await WorkRecordActions.MapAsync(tenant, farm, replacement, labourRepository, cancellationToken);
    }
}
