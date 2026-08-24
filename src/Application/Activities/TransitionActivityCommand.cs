using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed record TransitionActivityCommand(
    Guid ActivityId,
    string TargetStatus,
    long ExpectedVersion,
    string? Reason) : IRequest<ActivityDetailsDto>;
