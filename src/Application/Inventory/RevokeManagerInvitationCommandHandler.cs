using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class RevokeManagerInvitationCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<RevokeManagerInvitationCommand>
{
    public async Task Handle(RevokeManagerInvitationCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrower(tenant, userId);
        var invitation = (await inventoryRepository.GetManagerInvitationsAsync(
            tenant.Id, farm.Id, true, cancellationToken)).SingleOrDefault(item => item.Id == command.InvitationId)
            ?? throw new NotFoundException(command.InvitationId.ToString(), "Manager invitation");
        var now = timeProvider.GetUtcNow();
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () =>
            invitation.Revoke(now, userId, command.ExpectedVersion));
        InventoryAudit.Invitation(inventoryRepository, tenant, farm, user, invitation,
            "Revoked", now, null, "FarmManager invitation revoked before redemption.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
