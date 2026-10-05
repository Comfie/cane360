namespace Cane360.Application.Inventory;

public sealed class CreateInventoryLossCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user)
    : IRequestHandler<CreateInventoryLossCommand, Guid>
{
    public async Task<Guid> Handle(CreateInventoryLossCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);
        (Field Field, CropCycle Cycle, Activity Activity) context =
            InventoryAccess.RequireOperationalActivity(farm, command.ActivityId);
        StockIssueLine issueLine =
            await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, command.StockIssueLineId, false,
                cancellationToken) ??
            throw new NotFoundException(command.StockIssueLineId.ToString(), "Stock issue line");
        StockIssue issue =
            await inventoryRepository.GetStockIssueAsync(tenant.Id, farm.Id, issueLine.StockIssueId, false,
                cancellationToken) ?? throw new NotFoundException(issueLine.StockIssueId.ToString(), "Stock issue");
        InputRequest request =
            await inventoryRepository.GetInputRequestAsync(tenant.Id, farm.Id, issue.InputRequestId, false,
                cancellationToken) ?? throw new NotFoundException(issue.InputRequestId.ToString(), "Input request");
        if (issue.Status != StockIssueStatus.Posted || request.ActivityId != context.Activity.Id)
        {
            throw InventoryAccess.Failure(nameof(command.StockIssueLineId),
                "A loss must trace to a posted issue for this activity.");
        }

        InventoryLoss loss = InventoryAccess.ApplyDomainAction(nameof(command.Quantity),
            () => InventoryLoss.Create(tenant.Id, farm.Id, context.Activity.Id, issueLine, command.Quantity,
                command.LossType, command.Reason, userId));
        inventoryRepository.Add(loss);
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return loss.Id;
    }
}
