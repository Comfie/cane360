using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

public sealed record ActivityTypeDto(
    Guid Id,
    string Code,
    string Name,
    bool SupportsPlanned,
    bool SupportsUnplanned,
    string QuantityBasis,
    string Status,
    long Version);

public sealed record PersonRoleAssignmentDto(
    Guid Id,
    string Role,
    bool IsPrimary,
    string EffectiveFrom,
    string? EffectiveTo);

public sealed record PersonDto(
    Guid Id,
    string DisplayName,
    string? Phone,
    string ActiveFrom,
    string? ActiveTo,
    string Status,
    long Version,
    IReadOnlyList<PersonRoleAssignmentDto> Roles);

public sealed record PersonnelRegisterDto(
    bool PrimaryManagerAssigned,
    IReadOnlyList<PersonDto> Persons);

public sealed record FieldLineProfileDto(
    Guid Id,
    Guid FieldId,
    decimal StandardLineLengthMetres,
    int EstimatedLineCount,
    string NumberingScheme,
    string EffectiveFrom,
    string? EffectiveTo,
    long Version);

public sealed record ActivityListItemDto(
    Guid Id,
    Guid FieldId,
    string FieldCode,
    string FieldName,
    Guid CropCycleId,
    Guid ActivityTypeId,
    string ActivityTypeCode,
    string ActivityTypeName,
    string Kind,
    string? PlannedDate,
    string SupervisorName,
    string QuantityBasis,
    string? ActualAt,
    decimal? ActualQuantity,
    bool LineContextUnavailable,
    bool IsRetrospective,
    int EntryDelayDays,
    string? LateEntryReason,
    string Status,
    long Version,
    int SourceReferenceCount);

public sealed record ActivityCollectionDto(
    IReadOnlyList<ActivityListItemDto> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

public sealed record ActivityTimelineEventDto(
    Guid Id,
    string Type,
    string Title,
    string EventAt,
    string RecordedAt,
    string EnteredBy,
    string? OperationalActor,
    string? Detail,
    string? Reason);

public sealed record EvidenceLinkDto(
    Guid Id,
    string Role,
    string SourceSheetReference,
    string CapturedDate,
    string RecordedAt,
    string RecordedBy);

public sealed record ActivityDetailsDto(
    ActivityListItemDto Activity,
    IReadOnlyList<string> AllowedTransitions,
    IReadOnlyDictionary<string, string> BlockedTransitions,
    IReadOnlyList<ActivityTimelineEventDto> Timeline,
    IReadOnlyList<EvidenceLinkDto> SourceReferences);

internal static class ActivityMapper
{
    private static readonly ActivityStatus[] TransitionTargets =
    [
        ActivityStatus.Planned,
        ActivityStatus.Cancelled,
        ActivityStatus.InProgress,
        ActivityStatus.AwaitingVerification,
        ActivityStatus.ManagerConfirmation,
        ActivityStatus.Completed,
        ActivityStatus.Closed
    ];

    public static ActivityListItemDto MapListItem(Farm farm, Activity activity)
    {
        var field = farm.Fields.Single(item => item.Id == activity.FieldId);
        var supervisor = farm.Persons.Single(item => item.Id == activity.SupervisorPersonId);
        return new ActivityListItemDto(
            activity.Id,
            activity.FieldId,
            field.Code,
            field.Name,
            activity.CropCycleId,
            activity.ActivityTypeId,
            activity.ActivityTypeCode,
            activity.ActivityTypeName,
            activity.Kind.ToString(),
            activity.PlannedDate?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            supervisor.DisplayName,
            activity.QuantityBasis.ToString(),
            FormatTimestamp(activity.ActualAt),
            activity.ActualQuantity,
            activity.LineContextUnavailable,
            activity.IsRetrospective,
            activity.EntryDelayDays,
            activity.LateEntryReason,
            activity.Status.ToString(),
            activity.Version,
            activity.EvidenceLinks.Count);
    }

    public static async Task<ActivityDetailsDto> MapDetailsAsync(
        Tenant tenant,
        Activity activity,
        IIdentityService identityService,
        IReadOnlyList<WorkRecord>? labourRecords = null,
        IReadOnlyList<WorkerProfile>? workers = null)
    {
        var farm = tenant.ActiveFarm!;
        var allowed = TransitionTargets
            .Where(target => Activity.IsAllowedTransition(activity.Status, target))
            .Select(target => target.ToString())
            .ToArray();
        var blocked = new Dictionary<string, string>();
        if (activity.IsTerminal)
        {
            blocked["Modify"] = $"{FormatStatus(activity.Status)} activities are read-only.";
        }
        else if (activity.Status is ActivityStatus.AwaitingVerification or ActivityStatus.ManagerConfirmation)
        {
            blocked["ActualWork"] = "Return the activity to In progress before changing actual work.";
        }

        var userIds = activity.StatusChanges.Select(change => change.RecordedBy)
            .Append(activity.CreatedBy)
            .Append(activity.ActualEnteredByUserId)
            .Concat(activity.EvidenceLinks.Select(link => link.RecordedBy))
            .Concat((labourRecords ?? []).SelectMany(record => new[]
            {
                record.EnteredByUserId,
                record.Verification?.SupervisorVerificationEnteredByUserId,
                record.Verification?.ManagerConfirmedByUserId
            }))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>()
            .ToArray();
        var users = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var userId in userIds)
        {
            users[userId] = await identityService.GetUserNameAsync(userId) ?? "Unknown user";
        }

        string UserName(string? id) => id is not null && users.TryGetValue(id, out var name) ? name : "Unknown user";
        string? PersonName(Guid? id) => id is null
            ? null
            : farm.Persons.SingleOrDefault(person => person.Id == id)?.DisplayName;
        var timeline = new List<ActivityTimelineEventDto>
        {
            new(
                activity.Id,
                "ActivityCreated",
                $"{activity.Kind} activity recorded",
                FormatTimestamp(activity.Created),
                FormatTimestamp(activity.Created),
                UserName(activity.CreatedBy),
                PersonName(activity.SupervisorPersonId),
                activity.ActivityTypeName,
                null)
        };

        if (activity.ActualAt is not null && activity.ActualEnteredAt is not null)
        {
            timeline.Add(new ActivityTimelineEventDto(
                activity.Id,
                "ActualWork",
                "Actual work captured",
                FormatTimestamp(activity.ActualAt),
                FormatTimestamp(activity.ActualEnteredAt),
                UserName(activity.ActualEnteredByUserId),
                PersonName(activity.SupervisorPersonId),
                Coverage(activity),
                activity.LateEntryReason));
        }

        timeline.AddRange(activity.StatusChanges.Select(change => new ActivityTimelineEventDto(
            change.Id,
            "StatusChange",
            $"Moved to {FormatStatus(change.ToStatus)}",
            FormatTimestamp(change.RecordedAt),
            FormatTimestamp(change.RecordedAt),
            UserName(change.RecordedBy),
            PersonName(change.OperationalPersonId),
            change.ToStatus == ActivityStatus.ManagerConfirmation
                ? $"Entered by {UserName(change.RecordedBy)}; verification provided by {PersonName(change.OperationalPersonId)}."
                : null,
            change.Reason)));
        timeline.AddRange(activity.EvidenceLinks.Select(link => new ActivityTimelineEventDto(
            link.Id,
            "SourceReference",
            "Source-sheet reference added",
            link.CapturedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            FormatTimestamp(link.RecordedAt),
            UserName(link.RecordedBy),
            null,
            link.SourceSheetReference,
            null)));
        timeline.AddRange((labourRecords ?? []).Select(record =>
        {
            var worker = workers?.SingleOrDefault(candidate => candidate.Id == record.WorkerProfileId);
            var workerName = worker is null ? "Worker" : farm.Persons.Single(person => person.Id == worker.PersonId).DisplayName;
            var actor = record.Verification is null ? null : PersonName(record.Verification.SupervisorPersonId);
            var detail = record.Status == WorkRecordStatus.Confirmed
                ? $"{workerName} · {record.PayBasis} · confirmed labour evidence"
                : $"{workerName} · {record.PayBasis} · {record.Status}";
            return new ActivityTimelineEventDto(
                record.Id,
                "LabourEvidence",
                "Labour evidence recorded",
                record.WorkDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                FormatTimestamp(record.Verification?.ManagerConfirmedAt ?? record.Verification?.SupervisorVerifiedAt ?? record.EnteredAt),
                UserName(record.Verification?.ManagerConfirmedByUserId ?? record.Verification?.SupervisorVerificationEnteredByUserId ?? record.EnteredByUserId),
                actor,
                detail,
                record.LateEntryReason);
        }));

        return new ActivityDetailsDto(
            MapListItem(farm, activity),
            allowed,
            blocked,
            timeline.OrderByDescending(item => item.EventAt, StringComparer.Ordinal)
                .ThenByDescending(item => item.RecordedAt, StringComparer.Ordinal).ToArray(),
            activity.EvidenceLinks.OrderByDescending(link => link.RecordedAt).Select(link => new EvidenceLinkDto(
                link.Id,
                link.Role.ToString(),
                link.SourceSheetReference,
                link.CapturedDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                FormatTimestamp(link.RecordedAt),
                UserName(link.RecordedBy))).ToArray());
    }

    public static string Coverage(Activity activity) => activity.QuantityBasis switch
    {
        ActivityQuantityBasis.None => "No quantity basis",
        ActivityQuantityBasis.Hectares => $"{activity.ActualQuantity:N4} ha",
        ActivityQuantityBasis.StandardLines when activity.LineContextUnavailable =>
            $"{activity.ActualQuantity:N0} standard lines · line context unavailable",
        ActivityQuantityBasis.StandardLines => $"{activity.ActualQuantity:N0} standard lines",
        _ => string.Empty
    };

    public static string FormatStatus(ActivityStatus status) => status switch
    {
        ActivityStatus.InProgress => "In progress",
        ActivityStatus.AwaitingVerification => "Awaiting verification",
        ActivityStatus.ManagerConfirmation => "Manager confirmation",
        _ => status.ToString()
    };

    public static string FormatTimestamp(DateTimeOffset? timestamp) =>
        timestamp?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture) ?? string.Empty;
}
