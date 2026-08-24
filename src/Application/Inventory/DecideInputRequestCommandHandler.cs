using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class DecideInputRequestCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<DecideInputRequestCommand>
{
    public async Task Handle(DecideInputRequestCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        var role = InventoryAccess.SecurityRole(tenant, userId);
        if (role is not (TenantSecurityRoles.Grower or TenantSecurityRoles.FarmManager))
            throw new ForbiddenAccessException();

        var request = await inventoryRepository.GetInputRequestAsync(
            tenant.Id, farm.Id, command.InputRequestId, true, cancellationToken)
            ?? throw new NotFoundException(command.InputRequestId.ToString(), "Input request");
        var existing = await inventoryRepository.GetInputRequestApprovalAsync(
            request.Id, command.ExpectedVersion, cancellationToken);
        if (existing is not null)
        {
            if (existing.IdempotencyKey == command.IdempotencyKey && existing.Outcome == command.Outcome) return;
            throw new ConflictException("This exact request version already has an approval decision.");
        }
        InventoryAccess.RequireOperationalActivity(farm, request.ActivityId);
        if (request.RequiresGrower && role != TenantSecurityRoles.Grower)
            throw new ForbiddenAccessException();

        var now = timeProvider.GetUtcNow();
        var decision = InventoryAccess.ApplyDomainAction(nameof(command.Outcome), () =>
            ApprovalDecision.CreateInputRequestDecision(tenant.Id, farm.Id, request.Id,
                command.ExpectedVersion, command.Outcome, userId, role, now,
                command.Reason, command.IdempotencyKey));
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () =>
            request.Decide(command.Outcome, command.Reason, now, command.ExpectedVersion));
        inventoryRepository.Add(decision);
        InventoryAudit.Request(inventoryRepository, tenant, farm, user, request,
            command.Outcome.ToString(), now, command.Reason,
            $"Request version {command.ExpectedVersion} received an immutable {role} decision.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
