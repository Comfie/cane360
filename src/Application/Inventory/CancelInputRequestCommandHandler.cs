namespace Cane360.Application.Inventory;

public sealed class CancelInputRequestCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CancelInputRequestCommand>
{
    public async Task Handle(CancelInputRequestCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);

        InputRequest request = await inventoryRepository.GetInputRequestAsync(
                                   tenant.Id, farm.Id, command.InputRequestId, true, cancellationToken)
                               ?? throw new NotFoundException(command.InputRequestId.ToString(), "Input request");
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () =>
            request.Cancel(command.Reason, command.ExpectedVersion));
        InventoryAudit.Request(inventoryRepository, tenant, farm, user, request, "Cancelled",
            timeProvider.GetUtcNow(), command.Reason, "Input request cancelled before approval.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
