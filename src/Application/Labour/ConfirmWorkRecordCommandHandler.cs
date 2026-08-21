using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class ConfirmWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<ConfirmWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(ConfirmWorkRecordCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var record = LabourAccess.RequireWorkRecord(
            await labourRepository.GetWorkRecordAsync(tenant.Id, farm.Id, request.WorkRecordId, true, cancellationToken), request.WorkRecordId);
        await WorkRecordActions.RevalidateEvidenceAsync(tenant, farm, record, labourRepository, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var userId = LabourAccess.RequireUserId(user);
        LabourAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () => record.Confirm(now, userId, request.ExpectedVersion));
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkRecord), record.Id,
            "ManagerConfirmed", userId, LabourAccess.SecurityRole(tenant, userId), record.Verification!.SupervisorPersonId,
            now, LabourAccess.CorrelationId(user), null,
            "Authenticated manager confirmed labour evidence for future payroll eligibility."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return await WorkRecordActions.MapAsync(tenant, farm, record, labourRepository, cancellationToken);
    }
}
