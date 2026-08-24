namespace Cane360.Application.Inventory;

public sealed class SubmitInventoryLossCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository, IUser user, TimeProvider timeProvider) : IRequestHandler<SubmitInventoryLossCommand>
{
    public async Task Handle(SubmitInventoryLossCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); InventoryAccess.RequireGrowerOrManager(tenant, InventoryAccess.RequireUserId(user));
        var loss = await inventoryRepository.GetInventoryLossAsync(tenant.Id, farm.Id, command.InventoryLossId, true, cancellationToken) ?? throw new NotFoundException(command.InventoryLossId.ToString(), "Inventory loss"); if (loss.Version != command.ExpectedVersion) throw new ConflictException("This loss changed after it was loaded. Refresh and try again.");
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () => loss.Submit(timeProvider.GetUtcNow(), command.ExpectedVersion)); await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
