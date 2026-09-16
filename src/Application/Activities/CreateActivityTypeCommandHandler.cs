using Cane360.Domain.Activities;
using Cane360.Domain.Farms;
using Cane360.Domain.Auditing;
using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Activities;

public sealed class CreateActivityTypeCommandHandler(IFarmSetupRepository repository, IUser user,
    TimeProvider clock)
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
        var farm = ActivityAccess.RequireFarm(tenant);
        var userId = ActivityAccess.RequireUserId(user);
        var membership = tenant.Memberships.Single(item => item.UserId == userId &&
            item.Status == RecordStatus.Active);
        repository.Add(AuditEvent.Create(tenant.Id, farm.Id, "ActivityType", type!.Id,
            "Created", userId, membership.SecurityRole, membership.PersonId,
            clock.GetUtcNow(), user.CorrelationId ?? Guid.NewGuid().ToString("N"), null,
            $"Activity type {type.Code} created."));
        await repository.SaveChangesAsync(cancellationToken);
        return GetActivityTypesQueryHandler.Map(type!);
    }
}
