namespace Cane360.Application.Activities;

public sealed class RecordActualWorkCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<RecordActualWorkCommand, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(RecordActualWorkCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireCaptureTenantAsync(repository, user, true, cancellationToken);
        Farm farm = ActivityAccess.RequireFarm(tenant);
        Activity activity = ActivityAccess.RequireAssignedActivity(tenant, user, request.ActivityId);
        ActivityAccess.RequireVersion(activity, request.ExpectedVersion);
        Field field = ActivityAccess.RequireField(farm, activity.FieldId);
        CropCycle cycle = ActivityAccess.RequireOperationalCycle(field, activity.CropCycleId);
        DateTimeOffset actualAtUtc = ActivityAccess.NormalizeUtc(request.ActualAt);
        DateOnly eventDate = ActivityAccess.HarareDate(actualAtUtc);
        IReadOnlyList<FarmSetting>? settings = await repository.GetFarmSettingsAsync(tenant.Id, farm.Id, false,
            cancellationToken);
        int lateEntryReasonAfterDays = (settings ?? []).SingleOrDefault(item =>
            item.Key == FarmSetting.ActivityLateEntryReasonDays && item.IsEffective(eventDate))?.Value ?? 2;
        ActivityAccess.RequireSupervisor(farm, activity.SupervisorPersonId, eventDate);
        FieldLineProfile? profile = field.LineProfiles.SingleOrDefault(candidate => candidate.IsEffective(eventDate));
        DateTimeOffset now = timeProvider.GetUtcNow();
        ActivityAccess.ApplyDomainAction(nameof(request.ActualAt), () => activity.RecordActualWork(
            actualAtUtc,
            request.ActualQuantity,
            field.ReportingHectares,
            profile,
            cycle.StartDate,
            now,
            ActivityAccess.RequireUserId(user),
            request.LateEntryReason,
            request.ExpectedVersion,
            lateEntryReasonAfterDays));
        await repository.SaveChangesAsync(cancellationToken);
        return await ActivityMapper.MapDetailsAsync(tenant, activity, identityService);
    }
}
