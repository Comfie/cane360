using System.Globalization;
using Cane360.Domain.Farms;
using Cane360.Domain.Activities;
using Cane360.Application.Activities;
using Cane360.Domain.Labour;

namespace Cane360.Application.CropCycles;

public sealed record CropCycleDetailsDto(
    CropCycleFieldDto Field,
    CropCycleListItemDto CropCycle,
    IReadOnlyList<string> AllowedTransitions,
    IReadOnlyDictionary<string, string> BlockedTransitions,
    IReadOnlyList<CropCycleTimelineEventDto> Timeline);
