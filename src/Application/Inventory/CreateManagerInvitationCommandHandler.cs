using System.Security.Cryptography;
using System.Text;

namespace Cane360.Application.Inventory;

public sealed class CreateManagerInvitationCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateManagerInvitationCommand, CreatedManagerInvitationDto>
{
    public async Task<CreatedManagerInvitationDto> Handle(
        CreateManagerInvitationCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);

        if (!TenantSecurityRoles.IsInvitable(command.Role))
        {
            throw InventoryAccess.Failure(nameof(command.Role),
                "Invitations may only grant FarmManager or Supervisor.");
        }

        if (command.ExpiresInHours is < 1 or > 168)
        {
            throw InventoryAccess.Failure(nameof(command.ExpiresInHours),
                "Invitation lifetime must be between 1 and 168 hours.");
        }

        bool isFarmManagerRole = command.Role == TenantSecurityRoles.FarmManager;
        Person person = InventoryAccess.RequireActivePerson(farm, command.PersonId,
            isFarmManagerRole ? "Farm manager" : "Supervisor");
        DateOnly today = InventoryAccess.HarareDate(timeProvider.GetUtcNow());
        bool holdsRole = isFarmManagerRole
            ? person.RoleAssignments.Any(role =>
                role.Role == PersonRole.FarmManager && role.IsPrimary && role.IsEffective(today))
            : person.RoleAssignments.Any(role => role.Role == PersonRole.Supervisor && role.IsEffective(today));
        if (!holdsRole)
        {
            throw InventoryAccess.Failure(nameof(command.PersonId),
                isFarmManagerRole
                    ? "Invitations are limited to the active primary FarmManager person."
                    : "The selected person must have an effective Supervisor role.");
        }

        string token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .TrimEnd('=').Replace('+', '-').Replace('/', '_');
        string hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
        DateTimeOffset now = timeProvider.GetUtcNow();
        ManagerInvitation invitation = ManagerInvitation.Create(
            tenant.Id, farm.Id, person.Id, hash, now.AddHours(command.ExpiresInHours), userId, command.Role);
        inventoryRepository.Add(invitation);
        InventoryAudit.Invitation(inventoryRepository, tenant, farm, user, invitation,
            "Created", now, null, $"Single-use {command.Role} invitation created; only its secure hash is retained.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return new CreatedManagerInvitationDto(invitation.Id, invitation.PersonId, invitation.ExpiresAt,
            invitation.Version, token);
    }
}
