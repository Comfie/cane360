namespace Cane360.Application.Activities;

public sealed record EndPersonRoleCommand(Guid PersonId, Guid AssignmentId, long ExpectedVersion, DateOnly EffectiveTo)
    : IRequest<PersonnelRegisterDto>;
