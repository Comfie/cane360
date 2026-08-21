using System.Globalization;
using Cane360.Domain.Farms;
using Cane360.Domain.Activities;
using Cane360.Application.Activities;
using Cane360.Domain.Labour;

namespace Cane360.Application.CropCycles;

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
