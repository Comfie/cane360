namespace Cane360.Application.Activities;

public sealed record ActivityDetailsDto(
    ActivityListItemDto Activity,
    IReadOnlyList<string> AllowedTransitions,
    IReadOnlyDictionary<string, string> BlockedTransitions,
    IReadOnlyList<ActivityTimelineEventDto> Timeline,
    IReadOnlyList<EvidenceLinkDto> SourceReferences);
