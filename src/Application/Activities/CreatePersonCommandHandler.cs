using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class CreatePersonCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<CreatePersonCommand, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        Person? person = null;
        ActivityAccess.ApplyDomainAction(nameof(request.DisplayName), () =>
        {
            person = farm.AddPerson(request.DisplayName, request.Phone, request.ActiveFrom);
            foreach (var roleName in request.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var role = Enum.Parse<PersonRole>(roleName, true);
                farm.AssignRole(person, role, role == PersonRole.FarmManager && request.IsPrimaryManager, request.ActiveFrom);
            }
        });
        await repository.SaveChangesAsync(cancellationToken);
        return GetPersonnelQueryHandler.Map(farm);
    }
}
