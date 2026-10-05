namespace Cane360.Application.Activities;

public sealed record AddSourceReferenceCommand(
    Guid ActivityId,
    long ExpectedVersion,
    string SourceSheetReference,
    DateOnly CapturedDate) : IRequest<ActivityDetailsDto>;
