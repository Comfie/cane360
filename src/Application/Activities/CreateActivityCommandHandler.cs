namespace Cane360.Application.Activities;

public sealed class CreateActivityCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<CreateActivityCommand, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireCaptureTenantAsync(repository, user, true, cancellationToken);
        Farm farm = ActivityAccess.RequireFarm(tenant);
        Field field = ActivityAccess.RequireField(farm, request.FieldId);
        CropCycle cycle = ActivityAccess.RequireOperationalCycle(field, request.CropCycleId);
        ActivityType type = tenant.ActivityTypes.SingleOrDefault(candidate =>
                                candidate.Id == request.ActivityTypeId && candidate.Status == RecordStatus.Active)
                            ?? throw new NotFoundException(request.ActivityTypeId.ToString(), "Active activity type");
        ActivityPlanningKind kind = Enum.Parse<ActivityPlanningKind>(request.Kind, true);
        DateOnly effectiveDate = request.PlannedDate ?? ActivityAccess.HarareDate(timeProvider.GetUtcNow());
        ActivityAccess.RequireAssignedSupervisor(tenant, user, request.SupervisorPersonId);
        ActivityAccess.RequireSupervisor(farm, request.SupervisorPersonId, effectiveDate);
        Activity? activity = null;
        ActivityAccess.ApplyDomainAction(nameof(request.Kind), () => activity = cycle.CreateActivity(
            tenant.Id, farm.Id, field.Id, type, kind, request.PlannedDate, request.SupervisorPersonId));
        await repository.SaveChangesAsync(cancellationToken);
        return await ActivityMapper.MapDetailsAsync(tenant, activity!, identityService);
    }
}
