namespace Cane360.Application.Activities;

public sealed class CreatePersonCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<CreatePersonCommand, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(CreatePersonCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        Farm farm = ActivityAccess.RequireFarm(tenant);
        Person? person = null;
        ActivityAccess.ApplyDomainAction(nameof(request.DisplayName), () =>
        {
            person = farm.AddPerson(request.DisplayName, request.Phone, request.ActiveFrom);
            foreach (string roleName in request.Roles.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                PersonRole role = Enum.Parse<PersonRole>(roleName, true);
                farm.AssignRole(person, role, role == PersonRole.FarmManager && request.IsPrimaryManager,
                    request.ActiveFrom);
            }
        });
        await repository.SaveChangesAsync(cancellationToken);
        return GetPersonnelQueryHandler.Map(farm);
    }
}
