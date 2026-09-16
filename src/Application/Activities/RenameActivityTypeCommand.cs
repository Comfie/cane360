namespace Cane360.Application.Activities;

public sealed record RenameActivityTypeCommand(Guid ActivityTypeId, string Name,
    long ExpectedVersion) : IRequest<ActivityTypeDto>;
