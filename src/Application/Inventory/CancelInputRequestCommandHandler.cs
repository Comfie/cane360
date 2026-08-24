using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class CancelInputRequestCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<CancelInputRequestCommand>
{
    public async Task Handle(CancelInputRequestCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        InventoryAccess.RequireGrowerOrManager(tenant, InventoryAccess.RequireUserId(user));
        var request = await inventoryRepository.GetInputRequestAsync(
            tenant.Id, farm.Id, command.InputRequestId, true, cancellationToken)
            ?? throw new NotFoundException(command.InputRequestId.ToString(), "Input request");
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () =>
            request.Cancel(command.Reason, command.ExpectedVersion));
        InventoryAudit.Request(inventoryRepository, tenant, farm, user, request, "Cancelled",
            timeProvider.GetUtcNow(), command.Reason, "Input request cancelled before approval.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
