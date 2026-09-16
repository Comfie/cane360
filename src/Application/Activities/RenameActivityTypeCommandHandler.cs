using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;

namespace Cane360.Application.Activities;

public sealed class RenameActivityTypeCommandHandler(IFarmSetupRepository repository,
    IUser user, TimeProvider clock) : IRequestHandler<RenameActivityTypeCommand, ActivityTypeDto>
{
    public async Task<ActivityTypeDto> Handle(RenameActivityTypeCommand request,
        CancellationToken cancellationToken)
    {
        Tenant tenant = await ActivityAccess.RequireTenantAsync(repository, user, true, cancellationToken);
        var type = tenant.ActivityTypes.SingleOrDefault(item => item.Id == request.ActivityTypeId)
            ?? throw new NotFoundException(request.ActivityTypeId.ToString(), "Activity type");
        if (type.Version != request.ExpectedVersion)
            throw new ConflictException("This activity type changed after it was loaded.");
        ActivityAccess.ApplyDomainAction(nameof(request.Name), () =>
            type.Rename(request.Name, request.ExpectedVersion));
        Farm farm = ActivityAccess.RequireFarm(tenant);
        string userId = ActivityAccess.RequireUserId(user);
        TenantMembership membership = tenant.Memberships.Single(item => item.UserId == userId &&
            item.Status == RecordStatus.Active);
        repository.Add(AuditEvent.Create(tenant.Id, farm.Id, "ActivityType", type.Id,
            "Renamed", userId, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), user.CorrelationId ?? Guid.NewGuid().ToString("N"), null,
            $"Activity type {type.Code} renamed; historical activity names are unchanged."));
        await repository.SaveChangesAsync(cancellationToken);
        return GetActivityTypesQueryHandler.Map(type);
    }
}
