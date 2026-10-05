namespace Cane360.Application.Activities;

public sealed record PersonDto(
    Guid Id,
    string DisplayName,
    string? Phone,
    string ActiveFrom,
    string? ActiveTo,
    string Status,
    long Version,
    IReadOnlyList<PersonRoleAssignmentDto> Roles);
