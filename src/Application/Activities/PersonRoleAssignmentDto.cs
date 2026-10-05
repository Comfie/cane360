namespace Cane360.Application.Activities;

public sealed record PersonRoleAssignmentDto(
    Guid Id,
    string Role,
    bool IsPrimary,
    string EffectiveFrom,
    string? EffectiveTo);
