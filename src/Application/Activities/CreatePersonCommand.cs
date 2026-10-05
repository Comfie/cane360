namespace Cane360.Application.Activities;

public sealed record CreatePersonCommand(
    string DisplayName,
    string? Phone,
    DateOnly ActiveFrom,
    IReadOnlyList<string> Roles,
    bool IsPrimaryManager) : IRequest<PersonnelRegisterDto>;
