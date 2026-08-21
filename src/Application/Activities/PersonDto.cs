using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

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
