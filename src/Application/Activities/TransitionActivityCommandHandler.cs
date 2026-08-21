using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class TransitionActivityCommandHandler(
    IFarmSetupRepository repository,
    ILabourRepository labourRepository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<TransitionActivityCommand, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(TransitionActivityCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var activity = ActivityAccess.RequireActivity(tenant, request.ActivityId);
        ActivityAccess.RequireVersion(activity, request.ExpectedVersion);
        var target = Enum.Parse<ActivityStatus>(request.TargetStatus, true);
        Guid? operationalPersonId = null;
        if (target == ActivityStatus.ManagerConfirmation)
        {
            var effectiveDate = activity.ActualAt.HasValue
                ? ActivityAccess.HarareDate(activity.ActualAt.Value)
                : ActivityAccess.HarareDate(timeProvider.GetUtcNow());
            ActivityAccess.RequireSupervisor(farm, activity.SupervisorPersonId, effectiveDate);
            operationalPersonId = activity.SupervisorPersonId;
        }

        var allRequiredLabourVerified = target != ActivityStatus.Closed ||
            !await labourRepository.HasIncompleteWorkForActivityAsync(
                tenant.Id, farm.Id, activity.Id, cancellationToken);

        ActivityAccess.ApplyDomainAction(nameof(request.TargetStatus), () => activity.Transition(
            target,
            timeProvider.GetUtcNow(),
            ActivityAccess.RequireUserId(user),
            operationalPersonId,
            request.Reason,
            request.ExpectedVersion,
            noUnaccountedControlledInput: true,
            allRequiredLabourVerified: allRequiredLabourVerified));
        await repository.SaveChangesAsync(cancellationToken);
        return await ActivityMapper.MapDetailsAsync(tenant, activity, identityService);
    }
}
