namespace Cane360.Application.Activities;

public sealed class TransitionActivityCommandHandler(
    IFarmSetupRepository repository,
    ILabourRepository labourRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<TransitionActivityCommand, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(TransitionActivityCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        Farm farm = ActivityAccess.RequireFarm(tenant);
        Activity activity = ActivityAccess.RequireActivity(tenant, request.ActivityId);
        ActivityAccess.RequireVersion(activity, request.ExpectedVersion);
        ActivityStatus target = Enum.Parse<ActivityStatus>(request.TargetStatus, true);
        Guid? operationalPersonId = null;
        if (target == ActivityStatus.ManagerConfirmation)
        {
            DateOnly effectiveDate = activity.ActualAt.HasValue
                ? ActivityAccess.HarareDate(activity.ActualAt.Value)
                : ActivityAccess.HarareDate(timeProvider.GetUtcNow());
            ActivityAccess.RequireSupervisor(farm, activity.SupervisorPersonId, effectiveDate);
            operationalPersonId = activity.SupervisorPersonId;
        }

        bool allRequiredLabourVerified = target != ActivityStatus.Closed ||
                                         !await labourRepository.HasIncompleteWorkForActivityAsync(
                                             tenant.Id, farm.Id, activity.Id, cancellationToken);

        if (target == ActivityStatus.Closed)
        {
            await using IInventoryTransaction transaction =
                await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
            await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, activity.Id, cancellationToken);
            bool hasBlockingInventoryException = await inventoryRepository.HasBlockingInventoryExceptionAsync(
                tenant.Id, farm.Id, activity.Id, cancellationToken);
            ActivityAccess.ApplyDomainAction(nameof(request.TargetStatus), () => activity.Transition(
                target, timeProvider.GetUtcNow(), ActivityAccess.RequireUserId(user), operationalPersonId,
                request.Reason, request.ExpectedVersion, !hasBlockingInventoryException, allRequiredLabourVerified));
            await repository.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return await ActivityMapper.MapDetailsAsync(tenant, activity, identityService);
        }

        ActivityAccess.ApplyDomainAction(nameof(request.TargetStatus), () => activity.Transition(
            target,
            timeProvider.GetUtcNow(),
            ActivityAccess.RequireUserId(user),
            operationalPersonId,
            request.Reason,
            request.ExpectedVersion,
            true,
            allRequiredLabourVerified));
        await repository.SaveChangesAsync(cancellationToken);
        return await ActivityMapper.MapDetailsAsync(tenant, activity, identityService);
    }
}
