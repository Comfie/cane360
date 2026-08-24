using System.Globalization;
using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Labour;

namespace Cane360.Application.Activities;

public sealed record PersonRoleAssignmentDto(
    Guid Id,
    string Role,
    bool IsPrimary,
    string EffectiveFrom,
    string? EffectiveTo);
