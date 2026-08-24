using Cane360.Domain.Activities;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Labour;

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
