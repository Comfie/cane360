namespace Cane360.Application.Activities;

public sealed record GetActivityTypesQuery : IRequest<IReadOnlyList<ActivityTypeDto>>;
