using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed record GetActivityDetailsQuery(Guid ActivityId) : IRequest<ActivityDetailsDto>;
