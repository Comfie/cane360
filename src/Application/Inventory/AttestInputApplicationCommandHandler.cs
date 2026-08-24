using Cane360.Domain.Activities;

namespace Cane360.Application.Inventory;

public sealed class AttestInputApplicationCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository, IUser user, TimeProvider timeProvider) : IRequestHandler<AttestInputApplicationCommand>
{
    public async Task Handle(AttestInputApplicationCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var candidate = await inventoryRepository.GetInputApplicationAsync(tenant.Id, farm.Id, command.InputApplicationId, false, cancellationToken) ?? throw new NotFoundException(command.InputApplicationId.ToString(), "Input application");
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken); await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidate.ActivityId, cancellationToken);
        var application = await inventoryRepository.GetInputApplicationAsync(tenant.Id, farm.Id, candidate.Id, true, cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Input application");
        if (application.Version != command.ExpectedVersion) throw new ConflictException("This application changed after it was loaded. Refresh and try again.");
        var supervisor = InventoryAccess.RequireActivePerson(farm, command.SupervisorPersonId, "Supervisor");
        if (!supervisor.HasEffectiveRole(PersonRole.Supervisor, InventoryAccess.HarareDate(application.AppliedAt))) throw InventoryAccess.Failure(nameof(command.SupervisorPersonId), "The named supervisor must have an effective Supervisor role.");
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => application.Attest(command.SupervisorPersonId, timeProvider.GetUtcNow(), userId, command.Note, command.ExpectedVersion));
        InventoryAudit.Application(inventoryRepository, tenant, farm, user, application, "SupervisorAttested",
            timeProvider.GetUtcNow(), command.Note, "Supervisor attestation was entered by an authenticated user.");
        await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
    }
}
