using Cane360.Domain.Auditing;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed class VerifyWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository,
    ILabourRepository labourRepository,
    IUser user,
    TimeProvider timeProvider)
    : IRequestHandler<VerifyWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(VerifyWorkRecordCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = LabourAccess.RequireFarm(tenant);
        WorkRecord record = LabourAccess.RequireWorkRecord(
            await labourRepository.GetWorkRecordAsync(tenant.Id, farm.Id, request.WorkRecordId, true,
                cancellationToken), request.WorkRecordId);
        Person supervisor = LabourAccess.RequirePerson(farm, request.SupervisorPersonId);
        if (!supervisor.HasEffectiveRole(PersonRole.Supervisor, record.WorkDate))
        {
            throw LabourAccess.Failure(nameof(request.SupervisorPersonId),
                "The named person must have an effective Supervisor role on the work date.");
        }

        DateTimeOffset now = timeProvider.GetUtcNow();
        string userId = LabourAccess.RequireUserId(user);
        LabourAccess.ApplyDomainAction(nameof(request.SupervisorPersonId), () => record.RecordSupervisorVerification(
            supervisor.Id, now, userId, request.ExpectedVersion));
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkRecord), record.Id,
            "SupervisorVerificationRecorded", userId, LabourAccess.SecurityRole(tenant, userId), supervisor.Id,
            now, LabourAccess.CorrelationId(user), null,
            $"Entered by manager; verification provided by {supervisor.DisplayName}."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return await WorkRecordActions.MapAsync(tenant, farm, record, labourRepository, cancellationToken);
    }
}
