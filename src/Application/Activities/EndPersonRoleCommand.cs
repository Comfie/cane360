using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed record EndPersonRoleCommand(Guid PersonId, Guid AssignmentId, long ExpectedVersion, DateOnly EffectiveTo) : IRequest<PersonnelRegisterDto>;
