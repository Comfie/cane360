using Cane360.Domain.Activities;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed record AddSourceReferenceCommand(
    Guid ActivityId,
    long ExpectedVersion,
    string SourceSheetReference,
    DateOnly CapturedDate) : IRequest<ActivityDetailsDto>;
