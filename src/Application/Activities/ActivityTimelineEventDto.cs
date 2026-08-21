using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

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
