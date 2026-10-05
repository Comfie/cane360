namespace Cane360.Application.Activities;

public sealed class AddSourceReferenceCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<AddSourceReferenceCommand, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(AddSourceReferenceCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireCaptureTenantAsync(repository, user, true, cancellationToken);
        Farm farm = ActivityAccess.RequireFarm(tenant);
        Activity activity = ActivityAccess.RequireAssignedActivity(tenant, user, request.ActivityId);
        ActivityAccess.RequireVersion(activity, request.ExpectedVersion);
        Field field = ActivityAccess.RequireField(farm, activity.FieldId);
        ActivityAccess.RequireOperationalCycle(field, activity.CropCycleId);
        DateTimeOffset now = timeProvider.GetUtcNow();
        ActivityAccess.ApplyDomainAction(nameof(request.SourceSheetReference), () => activity.AddSourceReference(
            request.SourceSheetReference,
            request.CapturedDate,
            now,
            ActivityAccess.RequireUserId(user),
            request.ExpectedVersion));
        await repository.SaveChangesAsync(cancellationToken);
        return await ActivityMapper.MapDetailsAsync(tenant, activity, identityService);
    }
}
