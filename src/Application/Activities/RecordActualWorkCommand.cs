using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed record RecordActualWorkCommand(
    Guid ActivityId,
    long ExpectedVersion,
    DateTimeOffset ActualAt,
    decimal? ActualQuantity,
    string? LateEntryReason) : IRequest<ActivityDetailsDto>;
