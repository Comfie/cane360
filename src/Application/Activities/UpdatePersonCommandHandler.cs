using Cane360.Domain.Activities;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class UpdatePersonCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<UpdatePersonCommand, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(UpdatePersonCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var person = farm.Persons.SingleOrDefault(candidate => candidate.Id == request.PersonId)
            ?? throw new NotFoundException(request.PersonId.ToString(), "Person");
        if (person.Version != request.ExpectedVersion)
        {
            throw new ConflictException("This personnel record changed after it was loaded. Refresh and try again.");
        }

        var role = Enum.Parse<PersonRole>(request.Role, true);
        ActivityAccess.ApplyDomainAction(nameof(request.RoleEffectiveFrom), () => farm.UpdatePerson(
            person,
            request.DisplayName,
            request.Phone,
            role,
            role == PersonRole.FarmManager && request.IsPrimaryManager,
            request.RoleEffectiveFrom,
            request.ExpectedVersion));
        await repository.SaveChangesAsync(cancellationToken);
        return GetPersonnelQueryHandler.Map(farm);
    }
}
