namespace Cane360.Application.Activities;

public sealed record UpdatePersonCommand(
    Guid PersonId,
    string DisplayName,
    string? Phone,
    string Role,
    bool IsPrimaryManager,
    DateOnly RoleEffectiveFrom,
    long ExpectedVersion) : IRequest<PersonnelRegisterDto>;
