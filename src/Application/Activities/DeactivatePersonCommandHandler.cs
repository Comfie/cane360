using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class DeactivatePersonCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<DeactivatePersonCommand, PersonnelRegisterDto>
{
    public async Task<PersonnelRegisterDto> Handle(DeactivatePersonCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var farm = ActivityAccess.RequireFarm(tenant);
        var person = farm.Persons.SingleOrDefault(candidate => candidate.Id == request.PersonId)
            ?? throw new NotFoundException(request.PersonId.ToString(), "Person");
        if (person.Version != request.ExpectedVersion) throw new ConflictException("This personnel record changed after it was loaded. Refresh and try again.");
        ActivityAccess.ApplyDomainAction(nameof(request.ActiveTo), () => person.Deactivate(request.ActiveTo, request.ExpectedVersion));
        await repository.SaveChangesAsync(cancellationToken);
        return GetPersonnelQueryHandler.Map(farm);
    }
}
