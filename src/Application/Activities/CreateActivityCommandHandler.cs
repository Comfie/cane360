using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class CreateActivityCommandHandler(
    IFarmSetupRepository repository,
    IUser user,
    IIdentityService identityService,
    TimeProvider timeProvider) : IRequestHandler<CreateActivityCommand, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(CreateActivityCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var field = ActivityAccess.RequireField(farm, request.FieldId);
        var cycle = ActivityAccess.RequireOperationalCycle(field, request.CropCycleId);
        var type = tenant.ActivityTypes.SingleOrDefault(candidate =>
            candidate.Id == request.ActivityTypeId && candidate.Status == RecordStatus.Active)
            ?? throw new NotFoundException(request.ActivityTypeId.ToString(), "Active activity type");
        var kind = Enum.Parse<ActivityPlanningKind>(request.Kind, true);
        var effectiveDate = request.PlannedDate ?? ActivityAccess.HarareDate(timeProvider.GetUtcNow());
        ActivityAccess.RequireSupervisor(farm, request.SupervisorPersonId, effectiveDate);
        Activity? activity = null;
        ActivityAccess.ApplyDomainAction(nameof(request.Kind), () => activity = cycle.CreateActivity(
            tenant.Id, farm.Id, field.Id, type, kind, request.PlannedDate, request.SupervisorPersonId));
        await repository.SaveChangesAsync(cancellationToken);
        return await ActivityMapper.MapDetailsAsync(tenant, activity!, identityService);
    }
}
