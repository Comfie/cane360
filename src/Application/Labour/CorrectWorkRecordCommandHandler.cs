using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class CorrectWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<CorrectWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(CorrectWorkRecordCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var original = LabourAccess.RequireWorkRecord(
            await labourRepository.GetWorkRecordAsync(tenant.Id, farm.Id, request.WorkRecordId, true, cancellationToken), request.WorkRecordId);
        var userId = LabourAccess.RequireUserId(user);
        var now = timeProvider.GetUtcNow();
        LabourAccess.ApplyDomainAction(nameof(request.CorrectionReason), () => original.Supersede(
            request.CorrectionReason, userId, now, request.ExpectedVersion));
        var replacementRequest = new CreateWorkRecordCommand(original.WorkerProfileId, original.WorkDate,
            request.PayBasis, request.ActivityIds, request.Quantity, request.Scope, request.LateEntryReason);
        var replacement = await WorkRecordActions.BuildAsync(tenant, farm, labourRepository, user, timeProvider,
            replacementRequest, original.Id, cancellationToken);
        labourRepository.Add(replacement);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkRecord), original.Id,
            "EvidenceSuperseded", userId, LabourAccess.SecurityRole(tenant, userId), original.Verification?.SupervisorPersonId,
            now, LabourAccess.CorrelationId(user), request.CorrectionReason,
            "Labour evidence superseded by an explicit correction record."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return await WorkRecordActions.MapAsync(tenant, farm, replacement, labourRepository, cancellationToken);
    }
}
