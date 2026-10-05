namespace Cane360.Application.Activities;

public sealed record ActivityTypeDto(
    Guid Id,
    string Code,
    string Name,
    bool SupportsPlanned,
    bool SupportsUnplanned,
    string QuantityBasis,
    string Status,
    long Version);
