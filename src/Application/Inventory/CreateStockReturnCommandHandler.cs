using Cane360.Domain.Activities;

namespace Cane360.Application.Inventory;

public sealed class CreateStockReturnCommandHandler(IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository, IUser user)
    : IRequestHandler<CreateStockReturnCommand, Guid>
{
    public async Task<Guid> Handle(CreateStockReturnCommand command, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken); var farm = InventoryAccess.RequireFarm(tenant); var userId = InventoryAccess.RequireUserId(user); InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var context = InventoryAccess.RequireOperationalActivity(farm, command.ActivityId);
        var issueLines = new List<StockIssueLine>();
        foreach (var line in command.Lines)
        {
            var issueLine = await inventoryRepository.GetStockIssueLineAsync(tenant.Id, farm.Id, line.StockIssueLineId, false, cancellationToken) ?? throw new NotFoundException(line.StockIssueLineId.ToString(), "Stock issue line");
            var issue = await inventoryRepository.GetStockIssueAsync(tenant.Id, farm.Id, issueLine.StockIssueId, false, cancellationToken) ?? throw new NotFoundException(issueLine.StockIssueId.ToString(), "Stock issue");
            var request = await inventoryRepository.GetInputRequestAsync(tenant.Id, farm.Id, issue.InputRequestId, false, cancellationToken) ?? throw new NotFoundException(issue.InputRequestId.ToString(), "Input request");
            if (issue.Status != StockIssueStatus.Posted || request.ActivityId != command.ActivityId) throw InventoryAccess.Failure(nameof(command.Lines), "Returns must trace to posted issues for the selected activity.");
            issueLines.Add(issueLine);
        }
        await using var transaction = await inventoryRepository.BeginSerializableTransactionAsync(cancellationToken); await inventoryRepository.LockActivityAsync(tenant.Id, farm.Id, command.ActivityId, cancellationToken); await inventoryRepository.LockStoreAsync(tenant.Id, farm.Id, farm.Store.Id, cancellationToken); await inventoryRepository.LockStockIssueLinesAsync(issueLines.Select(x => x.Id).Order().ToArray(), cancellationToken);
        var receiver = InventoryAccess.RequireActivePerson(farm, command.ReceiverPersonId, "Storekeeper receiver"); if (!receiver.HasEffectiveRole(PersonRole.Storekeeper, command.ReturnDate)) throw InventoryAccess.Failure(nameof(command.ReceiverPersonId), "The return receiver must be an effective Storekeeper."); InventoryAccess.RequireActivePerson(farm, command.SenderPersonId, "Return sender");
        var stockReturn = StockReturn.Create(tenant.Id, farm.Id, farm.Store.Id, context.Activity.Id, command.ReturnDate, command.SenderPersonId, command.ReceiverPersonId);
        foreach (var line in command.Lines) { var issueLine = issueLines.Single(x => x.Id == line.StockIssueLineId); InventoryAccess.ApplyDomainAction(nameof(command.Lines), () => stockReturn.AddLine(issueLine, line.Quantity)); }
        inventoryRepository.Add(stockReturn); await inventoryRepository.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken); return stockReturn.Id;
    }
}
