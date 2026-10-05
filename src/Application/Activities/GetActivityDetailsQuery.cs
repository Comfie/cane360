namespace Cane360.Application.Activities;

public sealed record GetActivityDetailsQuery(Guid ActivityId) : IRequest<ActivityDetailsDto>;
