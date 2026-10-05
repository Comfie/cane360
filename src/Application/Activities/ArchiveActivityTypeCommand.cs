namespace Cane360.Application.Activities;

public sealed record ArchiveActivityTypeCommand(Guid ActivityTypeId, long ExpectedVersion) : IRequest<ActivityTypeDto>;
