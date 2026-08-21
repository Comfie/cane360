using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class AddSourceReferenceCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<AddSourceReferenceCommand, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(AddSourceReferenceCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var activity = ActivityAccess.RequireActivity(tenant, request.ActivityId);
        ActivityAccess.RequireVersion(activity, request.ExpectedVersion);
        var field = ActivityAccess.RequireField(farm, activity.FieldId);
        ActivityAccess.RequireOperationalCycle(field, activity.CropCycleId);
        var now = timeProvider.GetUtcNow();
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
