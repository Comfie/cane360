using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Auditing;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class ArchiveActivityTypeCommandHandler(IFarmSetupRepository repository, IUser user,
    TimeProvider clock)
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
        var farm = ActivityAccess.RequireFarm(tenant);
        var userId = ActivityAccess.RequireUserId(user);
        var membership = tenant.Memberships.Single(item => item.UserId == userId &&
            item.Status == RecordStatus.Active);
        repository.Add(AuditEvent.Create(tenant.Id, farm.Id, "ActivityType", type.Id,
            "Archived", userId, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), user.CorrelationId ?? Guid.NewGuid().ToString("N"), null,
            $"Activity type {type.Code} archived; historical activities retain their reference."));
        await repository.SaveChangesAsync(cancellationToken);
        return GetActivityTypesQueryHandler.Map(type);
    }
}
