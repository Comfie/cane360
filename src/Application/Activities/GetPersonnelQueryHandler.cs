using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class GetPersonnelQueryHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<GetPersonnelQuery, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(GetPersonnelQuery request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, false, cancellationToken);
        return Map(ActivityAccess.RequireFarm(tenant));
    }

    internal static PersonnelRegisterDto Map(Farm farm) => new(
        farm.Persons.Any(person => person.RoleAssignments.Any(role =>
            role.Role == PersonRole.FarmManager && role.IsPrimary && role.EffectiveTo is null)),
        farm.Persons.OrderBy(person => person.DisplayName).Select(person => new PersonDto(
            person.Id,
            person.DisplayName,
            person.Phone,
            person.ActiveFrom.ToString("yyyy-MM-dd"),
            person.ActiveTo?.ToString("yyyy-MM-dd"),
            person.Status.ToString(),
            person.Version,
            person.RoleAssignments.OrderBy(role => role.Role).Select(role => new PersonRoleAssignmentDto(
                role.Id,
                role.Role.ToString(),
                role.IsPrimary,
                role.EffectiveFrom.ToString("yyyy-MM-dd"),
                role.EffectiveTo?.ToString("yyyy-MM-dd"))).ToArray())).ToArray());
}
