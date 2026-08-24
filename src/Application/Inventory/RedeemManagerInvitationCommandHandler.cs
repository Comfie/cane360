using System.Security.Cryptography;
using System.Text;
using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Activities;

namespace Cane360.Application.Inventory;

public sealed class RedeemManagerInvitationCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<RedeemManagerInvitationCommand, TenantSessionDto>
{
    public async Task<TenantSessionDto> Handle(
        RedeemManagerInvitationCommand command, CancellationToken cancellationToken)
    {
        var userId = InventoryAccess.RequireUserId(user);
        if (string.IsNullOrWhiteSpace(command.Token))
            throw InventoryAccess.Failure(nameof(command.Token), "Invitation token is required.");
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(command.Token.Trim())));
        var invitation = await inventoryRepository.GetManagerInvitationByHashAsync(hash, true, cancellationToken)
            ?? throw new NotFoundException("token", "Manager invitation");
        var tenant = await farmRepository.GetTenantAsync(invitation.TenantId, true, cancellationToken)
            ?? throw new NotFoundException(invitation.TenantId.ToString(), "Invitation tenant");
        var farm = tenant.ActiveFarm;
        if (farm is null || farm.Id != invitation.FarmId)
            throw new NotFoundException(invitation.FarmId.ToString(), "Invitation farm");
        var manager = farm.Persons.SingleOrDefault(person => person.Id == invitation.PersonId)
            ?? throw new NotFoundException(invitation.PersonId.ToString(), "Invitation manager");
        var now = timeProvider.GetUtcNow();
        var today = InventoryAccess.HarareDate(now);
        if (!manager.HasEffectiveRole(PersonRole.FarmManager, today) ||
            !manager.RoleAssignments.Any(role => role.Role == PersonRole.FarmManager && role.IsPrimary && role.IsEffective(today)))
            throw InventoryAccess.Failure(nameof(command.Token), "The invitation no longer targets the active primary FarmManager.");

        InventoryAccess.ApplyDomainAction(nameof(command.Token), () => invitation.Redeem(now, userId));
        InventoryAccess.ApplyDomainAction(nameof(command.Token), () => tenant.AddFarmManagerMembership(userId, manager.Id));
        InventoryAudit.Invitation(inventoryRepository, tenant, farm, user, invitation,
            "Redeemed", now, null, "Invitation redeemed and FarmManager tenant membership activated.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return new TenantSessionDto(tenant.Id, farm.Id,
            Cane360.Domain.Farms.TenantSecurityRoles.FarmManager, manager.Id, manager.DisplayName);
    }
}
