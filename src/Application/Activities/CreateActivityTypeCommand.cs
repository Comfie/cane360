namespace Cane360.Application.Activities;

public sealed record CreateActivityTypeCommand(
    string Code,
    string Name,
    bool SupportsPlanned,
    bool SupportsUnplanned,
    string QuantityBasis) : IRequest<ActivityTypeDto>;
