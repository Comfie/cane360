namespace Cane360.Application.Activities;

public sealed record DeactivatePersonCommand(Guid PersonId, long ExpectedVersion, DateOnly ActiveTo)
    : IRequest<PersonnelRegisterDto>;
