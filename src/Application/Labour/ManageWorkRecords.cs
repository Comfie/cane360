using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

public sealed record GetLabourReferenceDataQuery(DateOnly WorkDate) : IRequest<LabourReferenceDataDto>;
public sealed record GetWorkRecordsQuery(DateOnly? WorkDate, Guid? WorkerId, Guid? ActivityId) : IRequest<IReadOnlyList<WorkRecordDto>>;
public sealed record WorkScopeCommand(string Type, int? StartLine, int? EndLine, string? SectionName);
public sealed record CreateWorkRecordCommand(
    Guid WorkerId,
    DateOnly WorkDate,
    string PayBasis,
    IReadOnlyList<Guid> ActivityIds,
    decimal? Quantity,
    WorkScopeCommand? Scope,
    string? LateEntryReason) : IRequest<WorkRecordDto>;
public sealed record VerifyWorkRecordCommand(Guid WorkRecordId, Guid SupervisorPersonId, long ExpectedVersion) : IRequest<WorkRecordDto>;
public sealed record ConfirmWorkRecordCommand(Guid WorkRecordId, long ExpectedVersion) : IRequest<WorkRecordDto>;
public sealed record CorrectWorkRecordCommand(
    Guid WorkRecordId,
    long ExpectedVersion,
    string CorrectionReason,
    string PayBasis,
    IReadOnlyList<Guid> ActivityIds,
    decimal? Quantity,
    WorkScopeCommand? Scope,
    string? LateEntryReason) : IRequest<WorkRecordDto>;

public sealed class CreateWorkRecordCommandValidator : AbstractValidator<CreateWorkRecordCommand>
{
    public CreateWorkRecordCommandValidator()
    {
        RuleFor(command => command.WorkerId).NotEmpty();
        RuleFor(command => command.WorkDate).NotEmpty();
        RuleFor(command => command.PayBasis).IsEnumName(typeof(PayBasis), false);
        RuleFor(command => command.ActivityIds).NotEmpty();
        RuleForEach(command => command.ActivityIds).NotEmpty();
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
        RuleFor(command => command.Scope!.Type).IsEnumName(typeof(WorkScopeType), false).When(command => command.Scope is not null);
        RuleFor(command => command.Scope!.SectionName).MaximumLength(120).When(command => command.Scope is not null);
    }
}

public sealed class CorrectWorkRecordCommandValidator : AbstractValidator<CorrectWorkRecordCommand>
{
    public CorrectWorkRecordCommandValidator()
    {
        RuleFor(command => command.WorkRecordId).NotEmpty();
        RuleFor(command => command.CorrectionReason).NotEmpty().MaximumLength(500);
        RuleFor(command => command.PayBasis).IsEnumName(typeof(PayBasis), false);
        RuleFor(command => command.ActivityIds).NotEmpty();
        RuleFor(command => command.LateEntryReason).MaximumLength(500);
    }
}

public sealed class VerifyWorkRecordCommandValidator : AbstractValidator<VerifyWorkRecordCommand>
{
    public VerifyWorkRecordCommandValidator()
    {
        RuleFor(command => command.WorkRecordId).NotEmpty();
        RuleFor(command => command.SupervisorPersonId).NotEmpty();
    }
}

public sealed class ConfirmWorkRecordCommandValidator : AbstractValidator<ConfirmWorkRecordCommand>
{
    public ConfirmWorkRecordCommandValidator() => RuleFor(command => command.WorkRecordId).NotEmpty();
}

public sealed class GetLabourReferenceDataQueryHandler(IFarmSetupRepository farmRepository, IUser user)
    : IRequestHandler<GetLabourReferenceDataQuery, LabourReferenceDataDto>
{
    public async Task<LabourReferenceDataDto> Handle(GetLabourReferenceDataQuery request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var activities = farm.Fields.SelectMany(field => field.CropCycles).Where(cycle => cycle.AcceptsOperationalEntries)
            .SelectMany(cycle => cycle.Activities)
            .Where(activity => activity.ActualAt.HasValue && LabourAccess.HarareDate(activity.ActualAt.Value) == request.WorkDate && !activity.IsTerminal)
            .Select(activity => new LabourActivityDto(activity.Id, activity.ActivityTypeId, activity.ActivityTypeName, activity.FieldId,
                request.WorkDate, activity.QuantityBasis.ToString(), activity.Status.ToString())).ToArray();
        return new LabourReferenceDataDto(
            farm.Fields.Where(field => field.Status == RecordStatus.Active).Select(field => new LabourFieldDto(field.Id, field.Code, field.Name)).ToArray(),
            activities,
            farm.Persons.Where(person => person.HasEffectiveRole(PersonRole.Supervisor, request.WorkDate))
                .Select(person => new LabourPersonDto(person.Id, person.DisplayName)).ToArray());
    }
}

public sealed class GetWorkRecordsQueryHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user)
    : IRequestHandler<GetWorkRecordsQuery, IReadOnlyList<WorkRecordDto>>
{
    public async Task<IReadOnlyList<WorkRecordDto>> Handle(GetWorkRecordsQuery request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        if (request.WorkerId.HasValue)
        {
            LabourAccess.RequireWorker(await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId.Value, false, cancellationToken), request.WorkerId.Value);
        }
        if (request.ActivityId.HasValue) LabourAccess.RequireActivity(tenant, request.ActivityId.Value);
        var workers = (await labourRepository.GetWorkersAsync(tenant.Id, farm.Id, false, cancellationToken)).ToDictionary(worker => worker.Id);
        var records = await labourRepository.GetWorkRecordsAsync(tenant.Id, farm.Id, request.WorkDate, request.WorkerId, request.ActivityId, false, cancellationToken);
        return records.Select(record => LabourMapper.Work(tenant, farm, workers, record)).ToArray();
    }
}

public sealed class CreateWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<CreateWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(CreateWorkRecordCommand request, CancellationToken cancellationToken) =>
        await WorkRecordActions.CreateAsync(farmRepository, labourRepository, user, timeProvider, request, null, cancellationToken);
}

public sealed class VerifyWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<VerifyWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(VerifyWorkRecordCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var record = LabourAccess.RequireWorkRecord(
            await labourRepository.GetWorkRecordAsync(tenant.Id, farm.Id, request.WorkRecordId, true, cancellationToken), request.WorkRecordId);
        var supervisor = LabourAccess.RequirePerson(farm, request.SupervisorPersonId);
        if (!supervisor.HasEffectiveRole(PersonRole.Supervisor, record.WorkDate))
        {
            throw LabourAccess.Failure(nameof(request.SupervisorPersonId), "The named person must have an effective Supervisor role on the work date.");
        }
        var now = timeProvider.GetUtcNow();
        var userId = LabourAccess.RequireUserId(user);
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

public sealed class ConfirmWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<ConfirmWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(ConfirmWorkRecordCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var record = LabourAccess.RequireWorkRecord(
            await labourRepository.GetWorkRecordAsync(tenant.Id, farm.Id, request.WorkRecordId, true, cancellationToken), request.WorkRecordId);
        await WorkRecordActions.RevalidateEvidenceAsync(tenant, farm, record, labourRepository, cancellationToken);
        var now = timeProvider.GetUtcNow();
        var userId = LabourAccess.RequireUserId(user);
        LabourAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () => record.Confirm(now, userId, request.ExpectedVersion));
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkRecord), record.Id,
            "ManagerConfirmed", userId, LabourAccess.SecurityRole(tenant, userId), record.Verification!.SupervisorPersonId,
            now, LabourAccess.CorrelationId(user), null,
            "Authenticated manager confirmed labour evidence for future payroll eligibility."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return await WorkRecordActions.MapAsync(tenant, farm, record, labourRepository, cancellationToken);
    }
}

public sealed class CorrectWorkRecordCommandHandler(
    IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider)
    : IRequestHandler<CorrectWorkRecordCommand, WorkRecordDto>
{
    public async Task<WorkRecordDto> Handle(CorrectWorkRecordCommand request, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var original = LabourAccess.RequireWorkRecord(
            await labourRepository.GetWorkRecordAsync(tenant.Id, farm.Id, request.WorkRecordId, true, cancellationToken), request.WorkRecordId);
        var userId = LabourAccess.RequireUserId(user);
        var now = timeProvider.GetUtcNow();
        LabourAccess.ApplyDomainAction(nameof(request.CorrectionReason), () => original.Supersede(
            request.CorrectionReason, userId, now, request.ExpectedVersion));
        var replacementRequest = new CreateWorkRecordCommand(original.WorkerProfileId, original.WorkDate,
            request.PayBasis, request.ActivityIds, request.Quantity, request.Scope, request.LateEntryReason);
        var replacement = await WorkRecordActions.BuildAsync(tenant, farm, labourRepository, user, timeProvider,
            replacementRequest, original.Id, cancellationToken);
        labourRepository.Add(replacement);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkRecord), original.Id,
            "EvidenceSuperseded", userId, LabourAccess.SecurityRole(tenant, userId), original.Verification?.SupervisorPersonId,
            now, LabourAccess.CorrelationId(user), request.CorrectionReason,
            "Labour evidence superseded by an explicit correction record."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return await WorkRecordActions.MapAsync(tenant, farm, replacement, labourRepository, cancellationToken);
    }
}

internal static class WorkRecordActions
{
    public static async Task<WorkRecordDto> CreateAsync(
        IFarmSetupRepository farmRepository, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider,
        CreateWorkRecordCommand request, Guid? correctsId, CancellationToken cancellationToken)
    {
        var tenant = await LabourAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = LabourAccess.RequireFarm(tenant);
        var record = await BuildAsync(tenant, farm, labourRepository, user, timeProvider, request, correctsId, cancellationToken);
        labourRepository.Add(record);
        var userId = LabourAccess.RequireUserId(user);
        labourRepository.Add(AuditEvent.Create(tenant.Id, farm.Id, nameof(WorkRecord), record.Id,
            "WorkEvidenceEntered", userId, LabourAccess.SecurityRole(tenant, userId), null,
            timeProvider.GetUtcNow(), LabourAccess.CorrelationId(user), request.LateEntryReason,
            "Labour evidence entered with an event-date rate snapshot."));
        await labourRepository.SaveChangesAsync(cancellationToken);
        return await MapAsync(tenant, farm, record, labourRepository, cancellationToken);
    }

    public static async Task<WorkRecord> BuildAsync(
        Tenant tenant, Farm farm, ILabourRepository labourRepository, IUser user, TimeProvider timeProvider,
        CreateWorkRecordCommand request, Guid? correctsId, CancellationToken cancellationToken)
    {
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, request.WorkerId, false, cancellationToken), request.WorkerId);
        if (!worker.IsActiveOn(request.WorkDate)) throw LabourAccess.Failure(nameof(request.WorkerId), "The worker is not active on the work date.");
        var attendance = LabourAccess.RequireAttendance(
            await labourRepository.GetAttendanceAsync(tenant.Id, farm.Id, worker.Id, request.WorkDate, false, cancellationToken), worker.Id, request.WorkDate);
        if (attendance.Status != AttendanceStatus.Present || attendance.FieldId is null)
        {
            throw LabourAccess.Failure(nameof(request.WorkerId), "Paid work requires Present attendance with one field allocation.");
        }

        var activities = request.ActivityIds.Distinct().Select(activityId => LabourAccess.RequireActivity(tenant, activityId)).ToArray();
        if (activities.Length != request.ActivityIds.Count) throw LabourAccess.Failure(nameof(request.ActivityIds), "An activity can be linked only once.");
        foreach (var activity in activities)
        {
            if (activity.FieldId != attendance.FieldId) throw LabourAccess.Failure(nameof(request.ActivityIds), "Every activity must use the attendance field allocation.");
            if (!activity.ActualAt.HasValue || LabourAccess.HarareDate(activity.ActualAt.Value) != request.WorkDate)
                throw LabourAccess.Failure(nameof(request.ActivityIds), "Every activity must have actual work on the attendance date.");
            if (activity.IsTerminal) throw LabourAccess.Failure(nameof(request.ActivityIds), "Closed or cancelled activities cannot accept labour evidence.");
            LabourAccess.RequireOperationalCycle(farm, activity);
        }

        var basis = Enum.Parse<PayBasis>(request.PayBasis, true);
        if (basis is PayBasis.Hectare or PayBasis.StandardLine && activities.Length != 1)
            throw LabourAccess.Failure(nameof(request.ActivityIds), "Piece work must reference exactly one activity.");
        Guid? activityTypeId = basis is PayBasis.Hectare or PayBasis.StandardLine ? activities[0].ActivityTypeId : null;
        if ((basis == PayBasis.Hectare && activities[0].QuantityBasis != ActivityQuantityBasis.Hectares) ||
            (basis == PayBasis.StandardLine && activities[0].QuantityBasis != ActivityQuantityBasis.StandardLines))
            throw LabourAccess.Failure(nameof(request.PayBasis), "The pay basis must match the activity quantity basis.");
        var rates = await labourRepository.GetRatesAsync(tenant.Id, farm.Id, worker.Id, false, cancellationToken);
        var applicable = rates.Where(rate => rate.Basis == basis && rate.ActivityTypeId == activityTypeId && rate.AppliesOn(request.WorkDate)).ToArray();
        if (applicable.Length != 1)
            throw LabourAccess.Failure(nameof(request.PayBasis), applicable.Length == 0
                ? "No effective rate exists for this worker, work date, and scope."
                : "Multiple effective rates exist for this worker, work date, and scope.");

        if (basis is PayBasis.Hectare or PayBasis.StandardLine && request.Scope is null)
            throw LabourAccess.Failure(nameof(request.Scope), "Piece work requires a line range or named work section.");
        var quantity = request.Quantity;
        if (basis == PayBasis.StandardLine && request.Scope is { StartLine: not null, EndLine: not null } &&
            string.Equals(request.Scope.Type, nameof(WorkScopeType.LineRange), StringComparison.OrdinalIgnoreCase))
            quantity = request.Scope.EndLine.Value - request.Scope.StartLine.Value + 1;
        var now = timeProvider.GetUtcNow();
        var delay = LabourAccess.EntryDelay(request.WorkDate, now);
        WorkRecord? record = null;
        LabourAccess.ApplyDomainAction(nameof(request.Quantity), () => record = WorkRecord.Create(
            tenant.Id, farm.Id, attendance.Id, worker.Id, attendance.FieldId.Value, request.WorkDate,
            applicable[0], quantity, activities.Select(activity => activity.Id).ToArray(), now,
            LabourAccess.RequireUserId(user), request.LateEntryReason, delay, correctsId));

        if (request.Scope is not null)
        {
            var scopeType = Enum.Parse<WorkScopeType>(request.Scope.Type, true);
            var activity = activities[0];
            if (scopeType == WorkScopeType.LineRange)
            {
                if (basis != PayBasis.StandardLine || request.Scope.StartLine is null || request.Scope.EndLine is null)
                    throw LabourAccess.Failure(nameof(request.Scope), "A standard-line range requires start and end lines.");
                var field = LabourAccess.RequireField(farm, attendance.FieldId.Value);
                var profile = field.LineProfiles.SingleOrDefault(candidate => candidate.IsEffective(request.WorkDate))
                    ?? throw LabourAccess.Failure(nameof(request.Scope), "No effective standard-line profile exists for the allocated field and work date.");
                if (request.Scope.EndLine > profile.EstimatedLineCount)
                    throw LabourAccess.Failure(nameof(request.Scope), "The line range exceeds the field's effective estimated line count.");
                LabourAccess.ApplyDomainAction(nameof(request.Scope), () => record!.AddLineRange(activity.Id, profile.Id,
                    request.Scope.StartLine.Value, request.Scope.EndLine.Value));
            }
            else
            {
                LabourAccess.ApplyDomainAction(nameof(request.Scope), () => record!.AddNamedSection(activity.Id, request.Scope.SectionName!));
            }

            var existing = await labourRepository.GetWorkRecordsAsync(tenant.Id, farm.Id, null, null, activity.Id, false, cancellationToken);
            var activeExisting = existing.Where(item => item.Id != correctsId).ToArray();
            ValidateScopeOverlap(record!, activeExisting);
            var usedQuantity = activeExisting.Where(item => item.Status is not (WorkRecordStatus.Cancelled or WorkRecordStatus.Superseded))
                .Sum(item => item.Quantity ?? 0);
            if (activity.ActualQuantity.HasValue && usedQuantity + record!.Quantity!.Value > activity.ActualQuantity.Value)
                throw LabourAccess.Failure(nameof(request.Quantity), "Claimed piece work cannot exceed the activity's actual quantity.");
        }

        return record!;
    }

    private static void ValidateScopeOverlap(WorkRecord candidate, IReadOnlyList<WorkRecord> existing)
    {
        var scope = candidate.Scopes.Single();
        var activeScopes = existing.Where(record => record.Status is not (WorkRecordStatus.Cancelled or WorkRecordStatus.Superseded))
            .SelectMany(record => record.Scopes).Where(item => item.ActivityId == scope.ActivityId && item.SupersededAt is null);
        if (scope.ScopeType == WorkScopeType.NamedSection && activeScopes.Any(item => item.ScopeType == WorkScopeType.NamedSection && item.NormalizedSectionName == scope.NormalizedSectionName))
            throw LabourAccess.Failure(nameof(CreateWorkRecordCommand.Scope), "This named work section is already claimed for the activity.");
        if (scope.ScopeType == WorkScopeType.LineRange && activeScopes.Any(item => item.ScopeType == WorkScopeType.LineRange &&
            scope.StartLine <= item.EndLine && item.StartLine <= scope.EndLine))
            throw LabourAccess.Failure(nameof(CreateWorkRecordCommand.Scope), "One or more standard lines are already claimed for this activity.");
    }

    public static async Task RevalidateEvidenceAsync(
        Tenant tenant, Farm farm, WorkRecord record, ILabourRepository labourRepository, CancellationToken cancellationToken)
    {
        var attendance = LabourAccess.RequireAttendance(
            await labourRepository.GetAttendanceAsync(tenant.Id, farm.Id, record.WorkerProfileId, record.WorkDate, false, cancellationToken),
            record.WorkerProfileId, record.WorkDate);
        if (attendance.Status != AttendanceStatus.Present || attendance.FieldId != record.FieldId)
            throw LabourAccess.Failure(nameof(record.AttendanceId), "The original Present attendance and field allocation are no longer compatible.");
        foreach (var link in record.Activities)
        {
            var activity = LabourAccess.RequireActivity(tenant, link.ActivityId);
            LabourAccess.RequireOperationalCycle(farm, activity);
            if (activity.FieldId != record.FieldId) throw LabourAccess.Failure(nameof(record.Activities), "The activity no longer matches the attendance field.");
        }
    }

    public static async Task<WorkRecordDto> MapAsync(
        Tenant tenant, Farm farm, WorkRecord record, ILabourRepository labourRepository, CancellationToken cancellationToken)
    {
        var worker = LabourAccess.RequireWorker(
            await labourRepository.GetWorkerAsync(tenant.Id, farm.Id, record.WorkerProfileId, false, cancellationToken), record.WorkerProfileId);
        return LabourMapper.Work(tenant, farm, new Dictionary<Guid, WorkerProfile> { [worker.Id] = worker }, record);
    }
}
