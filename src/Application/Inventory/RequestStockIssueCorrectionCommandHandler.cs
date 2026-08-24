using Cane360.Application.Common.Exceptions;

namespace Cane360.Application.Inventory;

public sealed class RequestStockIssueCorrectionCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<RequestStockIssueCorrectionCommand>
{
    public async Task Handle(RequestStockIssueCorrectionCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var issue = await inventoryRepository.GetStockIssueAsync(tenant.Id, farm.Id,
            command.StockIssueId, true, cancellationToken)
            ?? throw new NotFoundException(command.StockIssueId.ToString(), "Stock issue");
        InventoryAccess.ApplyDomainAction(nameof(command.ExpectedVersion), () =>
            issue.RequestCorrection(command.Reason, userId, command.ExpectedVersion));
        InventoryAudit.Issue(inventoryRepository, tenant, farm, user, issue, "CorrectionRequested",
            timeProvider.GetUtcNow(), command.Reason, "Issue correction was initiated; stock remains unchanged.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
    }
}
