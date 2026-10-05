namespace Cane360.Application.CropCycles;

public sealed record CropCycleDetailsDto(
    CropCycleFieldDto Field,
    CropCycleListItemDto CropCycle,
    IReadOnlyList<string> AllowedTransitions,
    IReadOnlyDictionary<string, string> BlockedTransitions,
    IReadOnlyList<CropCycleTimelineEventDto> Timeline);
