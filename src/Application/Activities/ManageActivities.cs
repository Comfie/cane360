using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed record GetActivitiesQuery(
    Guid? FieldId,
    Guid? CropCycleId,
    Guid? ActivityTypeId,
    string? Status,
    DateOnly? FromDate,
    DateOnly? ToDate,
    int Page = 1,
    int PageSize = 25) : IRequest<ActivityCollectionDto>;

public sealed record GetActivityDetailsQuery(Guid ActivityId) : IRequest<ActivityDetailsDto>;

public sealed record CreateActivityCommand(
    Guid FieldId,
    Guid CropCycleId,
    Guid ActivityTypeId,
    string Kind,
    DateOnly? PlannedDate,
    Guid SupervisorPersonId) : IRequest<ActivityDetailsDto>;

public sealed record RecordActualWorkCommand(
    Guid ActivityId,
    long ExpectedVersion,
    DateTimeOffset ActualAt,
    decimal? ActualQuantity,
    string? LateEntryReason) : IRequest<ActivityDetailsDto>;

public sealed record TransitionActivityCommand(
    Guid ActivityId,
    string TargetStatus,
    long ExpectedVersion,
    string? Reason) : IRequest<ActivityDetailsDto>;

public sealed record AddSourceReferenceCommand(
    Guid ActivityId,
    long ExpectedVersion,
    string SourceSheetReference,
    DateOnly CapturedDate) : IRequest<ActivityDetailsDto>;

public sealed class GetActivitiesQueryValidator : AbstractValidator<GetActivitiesQuery>
{
    public GetActivitiesQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThan(0);
        RuleFor(query => query.PageSize).InclusiveBetween(1, 100);
        RuleFor(query => query.ToDate).GreaterThanOrEqualTo(query => query.FromDate!.Value)
            .When(query => query.FromDate.HasValue && query.ToDate.HasValue);
        RuleFor(query => query.Status).IsEnumName(typeof(ActivityStatus), false)
            .When(query => !string.IsNullOrWhiteSpace(query.Status));
    }
}

public sealed class CreateActivityCommandValidator : AbstractValidator<CreateActivityCommand>
{
    public CreateActivityCommandValidator()
    {
        RuleFor(command => command.FieldId).NotEmpty();
        RuleFor(command => command.CropCycleId).NotEmpty();
        RuleFor(command => command.ActivityTypeId).NotEmpty();
        RuleFor(command => command.SupervisorPersonId).NotEmpty();
        RuleFor(command => command.Kind).IsEnumName(typeof(ActivityPlanningKind), false);
        RuleFor(command => command.PlannedDate).NotNull()
            .When(command => string.Equals(command.Kind, "Planned", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Planned work requires a planned date.");
    }
}

public sealed class RecordActualWorkCommandValidator : AbstractValidator<RecordActualWorkCommand>
{
    public RecordActualWorkCommandValidator()
    {
        RuleFor(command => command.ActivityId).NotEmpty();
        RuleFor(command => command.ActualAt).NotEmpty();
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
    }
}

public sealed class TransitionActivityCommandValidator : AbstractValidator<TransitionActivityCommand>
{
    public TransitionActivityCommandValidator()
    {
        RuleFor(command => command.TargetStatus).IsEnumName(typeof(ActivityStatus), false);
        RuleFor(command => command.Reason).MaximumLength(500);
    }
}

public sealed class AddSourceReferenceCommandValidator : AbstractValidator<AddSourceReferenceCommand>
{
    public AddSourceReferenceCommandValidator()
    {
        RuleFor(command => command.SourceSheetReference).NotEmpty().MaximumLength(160);
        RuleFor(command => command.CapturedDate).NotEmpty();
    }
}

public sealed class GetActivitiesQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetActivitiesQuery, ActivityCollectionDto>
{
    public async Task<ActivityCollectionDto> Handle(GetActivitiesQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        IEnumerable<Activity> query = farm.Fields.SelectMany(field => field.CropCycles).SelectMany(cycle => cycle.Activities);
        if (request.FieldId.HasValue) query = query.Where(activity => activity.FieldId == request.FieldId);
        if (request.CropCycleId.HasValue) query = query.Where(activity => activity.CropCycleId == request.CropCycleId);
        if (request.ActivityTypeId.HasValue) query = query.Where(activity => activity.ActivityTypeId == request.ActivityTypeId);
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var status = Enum.Parse<ActivityStatus>(request.Status, true);
            query = query.Where(activity => activity.Status == status);
        }
        if (request.FromDate.HasValue) query = query.Where(activity => OperationalDate(activity) >= request.FromDate);
        if (request.ToDate.HasValue) query = query.Where(activity => OperationalDate(activity) <= request.ToDate);

        var ordered = query.OrderByDescending(OperationalDate).ThenByDescending(activity => activity.Created).ToArray();
        var items = ordered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize)
            .Select(activity => ActivityMapper.MapListItem(farm, activity)).ToArray();
        return new ActivityCollectionDto(
            items,
            request.Page,
            request.PageSize,
            ordered.Length,
            (int)Math.Ceiling(ordered.Length / (double)request.PageSize));
    }

    private static DateOnly OperationalDate(Activity activity) => activity.ActualAt.HasValue
        ? ActivityAccess.HarareDate(activity.ActualAt.Value)
        : activity.PlannedDate ?? DateOnly.FromDateTime(activity.Created.UtcDateTime);
}

public sealed class GetActivityDetailsQueryHandler(
    IFarmSetupRepository repository,
    ILabourRepository labourRepository,
    IUser user,
    IIdentityService identityService) : IRequestHandler<GetActivityDetailsQuery, ActivityDetailsDto>
{
    public async Task<ActivityDetailsDto> Handle(GetActivityDetailsQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var records = await labourRepository.GetWorkRecordsAsync(
            tenant.Id, farm.Id, null, null, request.ActivityId, false, cancellationToken);
        var workers = await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken);
        return await ActivityMapper.MapDetailsAsync(
            tenant, ActivityAccess.RequireActivity(tenant, request.ActivityId), identityService, records, workers);
    }
}

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
