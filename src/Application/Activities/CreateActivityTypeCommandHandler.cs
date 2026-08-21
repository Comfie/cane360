using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class CreateActivityTypeCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<CreateActivityTypeCommand, ActivityTypeDto>
{
    public async Task<ActivityTypeDto> Handle(CreateActivityTypeCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        ActivityType? type = null;
        ActivityAccess.ApplyDomainAction(nameof(request.Code), () => type = tenant.AddActivityType(
            request.Code,
            request.Name,
            request.SupportsPlanned,
            request.SupportsUnplanned,
            Enum.Parse<ActivityQuantityBasis>(request.QuantityBasis)));
        await repository.SaveChangesAsync(cancellationToken);
        return GetActivityTypesQueryHandler.Map(type!);
    }
}
