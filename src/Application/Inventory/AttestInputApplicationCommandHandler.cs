namespace Cane360.Application.Inventory;

public sealed class AttestInputApplicationCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<AttestInputApplicationCommand>
{
    public async Task Handle(AttestInputApplicationCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);
        InputApplication candidate =
            await inventoryRepository.GetInputApplicationAsync(tenant.Id, farm.Id, command.InputApplicationId, false,
                cancellationToken) ??
            throw new NotFoundException(command.InputApplicationId.ToString(), "Input application");
        await using IInventoryTransaction transaction =
            await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken);
        await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, candidate.ActivityId, cancellationToken);
        InputApplication application =
            await inventoryRepository.GetInputApplicationAsync(tenant.Id, farm.Id, candidate.Id, true,
                cancellationToken) ?? throw new NotFoundException(candidate.Id.ToString(), "Input application");
        if (application.Version != command.ExpectedVersion)
        {
            throw new ConflictException("This application changed after it was loaded. Refresh and try again.");
        }

        Person supervisor = InventoryAccess.RequireActivePerson(farm, command.SupervisorPersonId, "Supervisor");
        if (!supervisor.HasEffectiveRole(PersonRole.Supervisor, InventoryAccess.HarareDate(application.AppliedAt)))
        {
            throw InventoryAccess.Failure(nameof(command.SupervisorPersonId),
                "The named supervisor must have an effective Supervisor role.");
        }

        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion),
            () => application.Attest(command.SupervisorPersonId, timeProvider.GetUtcNow(), userId, command.Note,
                command.ExpectedVersion));
        InventoryAudit.Application(inventoryRepository, tenant, farm, user, application, "SupervisorAttested",
            timeProvider.GetUtcNow(), command.Note, "Supervisor attestation was entered by an authenticated user.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
}
