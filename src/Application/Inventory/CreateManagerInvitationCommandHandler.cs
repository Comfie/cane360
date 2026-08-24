using System.Security.Cryptography;
using System.Text;
using Cane360.Domain.Activities;

namespace Cane360.Application.Inventory;

public sealed class CreateManagerInvitationCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<CreateManagerInvitationCommand, CreatedManagerInvitationDto>
{
    public async Task<CreatedManagerInvitationDto> Handle(
        CreateManagerInvitationCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrower(tenant, userId);
        if (command.ExpiresInHours is < 1 or > 168)
            throw InventoryAccess.Failure(nameof(command.ExpiresInHours), "Invitation lifetime must be between 1 and 168 hours.");
        var manager = InventoryAccess.RequireActivePerson(farm, command.PersonId, "Farm manager");
        var today = InventoryAccess.HarareDate(timeProvider.GetUtcNow());
        if (!manager.RoleAssignments.Any(role => role.Role == PersonRole.FarmManager && role.IsPrimary && role.IsEffective(today)))
            throw InventoryAccess.Failure(nameof(command.PersonId), "Invitations are limited to the active primary FarmManager person.");

        var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        var now = timeProvider.GetUtcNow();
        var invitation = Cane360.Domain.Farms.ManagerInvitation.Create(
            tenant.Id, farm.Id, manager.Id, hash, now.AddHours(command.ExpiresInHours), userId);
        inventoryRepository.Add(invitation);
        InventoryAudit.Invitation(inventoryRepository, tenant, farm, user, invitation,
            "Created", now, null, "Single-use FarmManager invitation created; only its secure hash is retained.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return new(invitation.Id, invitation.PersonId, invitation.ExpiresAt, invitation.Version, token);
    }
}
