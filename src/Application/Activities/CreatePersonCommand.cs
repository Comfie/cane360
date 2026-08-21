using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed record CreatePersonCommand(
    string DisplayName,
    string? Phone,
    DateOnly ActiveFrom,
    IReadOnlyList<string> Roles,
    bool IsPrimaryManager) : IRequest<PersonnelRegisterDto>;
