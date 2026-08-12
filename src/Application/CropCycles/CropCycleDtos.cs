using System.Globalization;
using Cane360.Domain.Farms;
using Cane360.Domain.Activities;
using Cane360.Application.Activities;

namespace Cane360.Application.CropCycles;

public sealed record CropVarietyDto(Guid Id, string Code, string Name);

public sealed record CropCycleFieldDto(Guid Id, string Code, string Name, decimal ReportingHectares);

public sealed record HarvestResultDto(string HarvestDate, decimal ActualTonnes);

public sealed record CropCycleTimelineEventDto(
    Guid Id,
    string Type,
    string Title,
    string EventDate,
    string RecordedAt,
    string? Detail,
    string? Reason,
    string? EnteredBy = null,
    string? OperationalActor = null);

public sealed record CropCycleListItemDto(
    Guid Id,
    string CycleType,
    int? RatoonNumber,
    Guid? CropVarietyId,
    string Variety,
    string StartDate,
    string ExpectedHarvestStart,
    string ExpectedHarvestEnd,
    decimal ExpectedYieldTonnes,
    string Status,
    long Version,
    HarvestResultDto? HarvestResult);

public sealed record CropCycleCollectionDto(
    CropCycleFieldDto Field,
    IReadOnlyList<CropCycleListItemDto> CropCycles);

public sealed record CropCycleDetailsDto(
    CropCycleFieldDto Field,
    CropCycleListItemDto CropCycle,
    IReadOnlyList<string> AllowedTransitions,
    IReadOnlyDictionary<string, string> BlockedTransitions,
    IReadOnlyList<CropCycleTimelineEventDto> Timeline);

internal static class CropCycleMapper
{
    public static CropCycleCollectionDto MapCollection(Field field) => new(
        MapField(field),
        field.CropCycles
            .OrderByDescending(cycle => cycle.StartDate)
            .ThenByDescending(cycle => cycle.Created)
            .Select(MapListItem)
            .ToArray());

    public static CropCycleDetailsDto MapDetails(Field field, CropCycle cycle)
    {
        var allowed = new List<string>();
        var blocked = new Dictionary<string, string>();

        switch (cycle.Status)
        {
            case CropCycleStatus.Draft:
                allowed.Add("Cancel");
                if (field.CurrentCropCycle is null)
                {
                    allowed.Insert(0, "Activate");
                }
                else
                {
                    blocked["Activate"] =
                        "Close or otherwise complete the field's current cycle before activating this draft.";
                }
                break;
            case CropCycleStatus.Active:
                allowed.Add("ReadyForHarvest");
                break;
            case CropCycleStatus.ReadyForHarvest:
                if (cycle.Activities.All(activity => activity.Status is ActivityStatus.Closed or ActivityStatus.Cancelled))
                {
                    allowed.Add("Harvest");
                }
                else
                {
                    blocked["Harvest"] = "Close or cancel every activity before recording harvest.";
                }
                break;
            case CropCycleStatus.Harvested:
                allowed.Add("Close");
                break;
            case CropCycleStatus.Closed:
                blocked["Modify"] = "Closed crop cycles are read-only.";
                break;
            case CropCycleStatus.Cancelled:
                blocked["Modify"] = "Cancelled crop cycles are read-only.";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(cycle.Status));
        }

        return new CropCycleDetailsDto(
            MapField(field),
            MapListItem(cycle),
            allowed,
            blocked,
            MapTimeline(field, cycle));
    }

    public static async Task<CropCycleDetailsDto> MapDetailsAsync(
        Field field,
        CropCycle cycle,
        Farm farm,
        IIdentityService identityService)
    {
        var details = MapDetails(field, cycle);
        var userIds = cycle.Activities
            .SelectMany(activity => activity.StatusChanges.Select(change => change.RecordedBy)
                .Append(activity.CreatedBy)
                .Append(activity.ActualEnteredByUserId)
                .Concat(activity.EvidenceLinks.Select(link => link.RecordedBy)))
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .Cast<string>();
        var users = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var userId in userIds)
        {
            users[userId] = await identityService.GetUserNameAsync(userId) ?? "Unknown user";
        }

        string UserName(string? id) => id is not null && users.TryGetValue(id, out var name) ? name : "Unknown user";
        string? PersonName(Guid? id) => id is null ? null : farm.Persons.SingleOrDefault(person => person.Id == id)?.DisplayName;
        var activityTimeline = cycle.Activities.SelectMany(activity =>
        {
            var entries = new List<CropCycleTimelineEventDto>
            {
                new(
                    activity.Id,
                    "ActivityCreated",
                    $"{activity.ActivityTypeName} activity recorded",
                    FormatTimestamp(activity.Created),
                    FormatTimestamp(activity.Created),
                    $"{activity.Kind} · {ActivityMapper.FormatStatus(activity.Status)}",
                    null,
                    UserName(activity.CreatedBy),
                    PersonName(activity.SupervisorPersonId))
            };
            if (activity.ActualAt is not null && activity.ActualEnteredAt is not null)
            {
                entries.Add(new CropCycleTimelineEventDto(
                    activity.Id,
                    "ActivityActualWork",
                    $"{activity.ActivityTypeName} actual work",
                    FormatTimestamp(activity.ActualAt.Value),
                    FormatTimestamp(activity.ActualEnteredAt.Value),
                    ActivityMapper.Coverage(activity),
                    activity.LateEntryReason,
                    UserName(activity.ActualEnteredByUserId),
                    PersonName(activity.SupervisorPersonId)));
            }
            entries.AddRange(activity.StatusChanges.Select(change => new CropCycleTimelineEventDto(
                change.Id,
                "ActivityStatusChange",
                $"{activity.ActivityTypeName} moved to {ActivityMapper.FormatStatus(change.ToStatus)}",
                FormatTimestamp(change.RecordedAt),
                FormatTimestamp(change.RecordedAt),
                null,
                change.Reason,
                UserName(change.RecordedBy),
                PersonName(change.OperationalPersonId))));
            entries.AddRange(activity.EvidenceLinks.Select(link => new CropCycleTimelineEventDto(
                link.Id,
                "ActivitySourceReference",
                $"Source reference added to {activity.ActivityTypeName}",
                FormatDate(link.CapturedDate),
                FormatTimestamp(link.RecordedAt),
                link.SourceSheetReference,
                null,
                UserName(link.RecordedBy),
                null)));
            return entries;
        });

        return details with
        {
            Timeline = details.Timeline.Concat(activityTimeline)
                .OrderByDescending(item => item.EventDate, StringComparer.Ordinal)
                .ThenByDescending(item => item.RecordedAt, StringComparer.Ordinal)
                .ToArray()
        };
    }

    private static CropCycleFieldDto MapField(Field field) =>
        new(field.Id, field.Code, field.Name, field.ReportingHectares);

    private static CropCycleListItemDto MapListItem(CropCycle cycle) => new(
        cycle.Id,
        cycle.CycleType.ToString(),
        cycle.RatoonNumber,
        cycle.CropVarietyId,
        cycle.Variety,
        FormatDate(cycle.StartDate),
        FormatDate(cycle.ExpectedHarvestStart),
        FormatDate(cycle.ExpectedHarvestEnd),
        cycle.ExpectedYieldTonnes,
        cycle.Status.ToString(),
        cycle.Version,
        cycle.HarvestResult is null
            ? null
            : new HarvestResultDto(
                FormatDate(cycle.HarvestResult.HarvestDate),
                cycle.HarvestResult.ActualTonnes));

    private static IReadOnlyList<CropCycleTimelineEventDto> MapTimeline(Field field, CropCycle cycle)
    {
        var timeline = new List<CropCycleTimelineEventDto>
        {
            new(
                field.Id,
                "FieldCreated",
                "Field created",
                FormatTimestamp(field.Created),
                FormatTimestamp(field.Created),
                $"{field.Code} · {field.Name}",
                null)
        };

        timeline.AddRange(cycle.StatusChanges.Select(change => new CropCycleTimelineEventDto(
            change.Id,
            "StatusChange",
            StatusTitle(change.FromStatus, change.ToStatus),
            FormatTimestamp(change.RecordedAt),
            FormatTimestamp(change.RecordedAt),
            change.ToStatus == CropCycleStatus.Draft
                ? $"{cycle.Variety} {FormatType(cycle)} cycle"
                : null,
            change.Reason)));

        if (cycle.HarvestResult is not null)
        {
            timeline.Add(new CropCycleTimelineEventDto(
                cycle.HarvestResult.Id,
                "HarvestResult",
                "Harvest result",
                FormatDate(cycle.HarvestResult.HarvestDate),
                FormatTimestamp(cycle.HarvestResult.Created),
                $"{cycle.HarvestResult.ActualTonnes:N3} actual tonnes",
                null));
        }

        return timeline
            .OrderByDescending(item => item.EventDate, StringComparer.Ordinal)
            .ThenByDescending(item => item.RecordedAt, StringComparer.Ordinal)
            .ToArray();
    }

    private static string StatusTitle(CropCycleStatus? fromStatus, CropCycleStatus toStatus) =>
        fromStatus is null
            ? $"Cycle recorded as {FormatStatus(toStatus)}"
            : $"Cycle moved to {FormatStatus(toStatus)}";

    private static string FormatType(CropCycle cycle) => cycle.CycleType switch
    {
        CropCycleType.PlantCane => "plant cane",
        CropCycleType.Ratoon => $"ratoon {cycle.RatoonNumber}",
        _ => cycle.CycleType.ToString()
    };

    private static string FormatStatus(CropCycleStatus status) => status switch
    {
        CropCycleStatus.ReadyForHarvest => "Ready for harvest",
        _ => status.ToString()
    };

    private static string FormatDate(DateOnly date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string FormatTimestamp(DateTimeOffset timestamp) =>
        timestamp.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture);
}
