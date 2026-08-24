using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class ArchiveActivityTypeCommandHandler(IFarmSetupRepository repository, IUser user)
    : IRequestHandler<ArchiveActivityTypeCommand, ActivityTypeDto>
{
    public async Task<ActivityTypeDto> Handle(ArchiveActivityTypeCommand request, CancellationToken cancellationToken)
    {
        var tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var type = tenant.ActivityTypes.SingleOrDefault(candidate => candidate.Id == request.ActivityTypeId)
            ?? throw new NotFoundException(request.ActivityTypeId.ToString(), "Activity type");
        if (type.Version != request.ExpectedVersion)
        {
            throw new ConflictException("This activity type changed after it was loaded. Refresh and try again.");
        }
        ActivityAccess.ApplyDomainAction(nameof(request.ExpectedVersion), () => type.Archive(request.ExpectedVersion));
        await repository.SaveChangesAsync(cancellationToken);
        return GetActivityTypesQueryHandler.Map(type);
    }
}
