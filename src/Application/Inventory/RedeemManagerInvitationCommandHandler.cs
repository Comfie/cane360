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
        var person = farm.Persons.SingleOrDefault(candidate => candidate.Id == invitation.PersonId)
            ?? throw new NotFoundException(invitation.PersonId.ToString(), "Invitation person");
        var now = timeProvider.GetUtcNow();
        var today = InventoryAccess.HarareDate(now);
        bool stillHoldsRole = invitation.SecurityRole == Cane360.Domain.Farms.TenantSecurityRoles.FarmManager
            ? person.RoleAssignments.Any(role => role.Role == PersonRole.FarmManager && role.IsPrimary && role.IsEffective(today))
            : person.RoleAssignments.Any(role => role.Role == PersonRole.Supervisor && role.IsEffective(today));
        if (!stillHoldsRole)
            throw InventoryAccess.Failure(nameof(command.Token), "The invitation no longer targets a person with that role.");

        InventoryAccess.ApplyDomainAction(nameof(command.Token), () => invitation.Redeem(now, userId));
        InventoryAccess.ApplyDomainAction(nameof(command.Token), () => tenant.AddMembership(userId, person.Id, invitation.SecurityRole));
        InventoryAudit.Invitation(inventoryRepository, tenant, farm, user, invitation,
            "Redeemed", now, null, "Invitation redeemed and tenant membership activated.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return new TenantSessionDto(tenant.Id, farm.Id, invitation.SecurityRole, person.Id, person.DisplayName);
    }
}
