using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class RecordActualWorkCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<RecordActualWorkCommand, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(RecordActualWorkCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var activity = ActivityAccess.RequireActivity(tenant, request.ActivityId);
        ActivityAccess.RequireVersion(activity, request.ExpectedVersion);
        var field = ActivityAccess.RequireField(farm, activity.FieldId);
        var cycle = ActivityAccess.RequireOperationalCycle(field, activity.CropCycleId);
        var actualAtUtc = ActivityAccess.NormalizeUtc(request.ActualAt);
        var eventDate = ActivityAccess.HarareDate(actualAtUtc);
        ActivityAccess.RequireSupervisor(farm, activity.SupervisorPersonId, eventDate);
        var profile = field.LineProfiles.SingleOrDefault(candidate => candidate.IsEffective(eventDate));
        var now = timeProvider.GetUtcNow();
        ActivityAccess.ApplyDomainAction(nameof(request.ActualAt), () => activity.RecordActualWork(
            actualAtUtc,
            request.ActualQuantity,
            field.ReportingHectares,
            profile,
            cycle.StartDate,
            now,
            ActivityAccess.RequireUserId(user),
            request.LateEntryReason,
            request.ExpectedVersion));
        await repository.SaveChangesAsync(cancellationToken);
        return await ActivityMapper.MapDetailsAsync(tenant, activity, identityService);
    }
}
