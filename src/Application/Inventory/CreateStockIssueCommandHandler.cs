namespace Cane360.Application.Inventory;

public sealed class CreateStockIssueCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateStockIssueCommand, Guid>
{
    public async Task<Guid> Handle(CreateStockIssueCommand command, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);

        InputRequest request = await inventoryRepository.GetInputRequestAsync(
                                   tenant.Id, farm.Id, command.InputRequestId, false, cancellationToken)
                               ?? throw new NotFoundException(command.InputRequestId.ToString(),
                                   "Approved input request");
        InventoryAccess.RequireOperationalActivity(farm, request.ActivityId);
        if (request.Status is not (InputRequestStatus.Approved or InputRequestStatus.PartiallyIssued))
        {
            throw InventoryAccess.Failure(nameof(command.InputRequestId), "Only an approved request may be issued.");
        }

        if (command.Lines.Count == 0)
        {
            throw InventoryAccess.Failure(nameof(command.Lines), "Add at least one issue line.");
        }

        Person issuer = InventoryAccess.RequireActivePerson(farm, command.IssuerPersonId, "Issuer");
        if (!issuer.HasEffectiveRole(PersonRole.Storekeeper, command.IssueDate))
        {
            throw InventoryAccess.Failure(nameof(command.IssuerPersonId),
                "The issuer must have an active Storekeeper role on the issue date.");
        }

        InventoryAccess.RequireActivePerson(farm, command.RecipientPersonId, "Field recipient");
        StockIssue issue = InventoryAccess.ApplyDomainAction(nameof(command.IssueDate), () => StockIssue.Create(
            tenant.Id, farm.Id, farm.Store.Id, request.Id, command.IssueDate,
            command.IssuerPersonId, command.RecipientPersonId, command.LateEntryReason,
            InventoryAccess.EntryDelay(command.IssueDate, timeProvider.GetUtcNow())));

        foreach (CreateStockIssueLineCommand requestedLine in command.Lines)
        {
            InputRequestLine requestLine =
                request.Lines.SingleOrDefault(line => line.Id == requestedLine.InputRequestLineId)
                ?? throw new NotFoundException(requestedLine.InputRequestLineId.ToString(), "Approved request line");
            InventoryLot? lot = null;
            if (requestedLine.InventoryLotId.HasValue)
            {
                lot = await inventoryRepository.GetLotAsync(tenant.Id, farm.Id,
                          requestedLine.InventoryLotId.Value, false, cancellationToken)
                      ?? throw new NotFoundException(requestedLine.InventoryLotId.Value.ToString(), "Inventory lot");
                if (lot.InventoryItemId != requestLine.InventoryItemId)
                {
                    throw InventoryAccess.Failure(nameof(requestedLine.InventoryLotId),
                        "The selected lot belongs to another item.");
                }
            }

            StockPosition position = await inventoryRepository.GetPositionAsync(tenant.Id, farm.Id, farm.Store.Id,
                                         requestLine.InventoryItemId, requestedLine.InventoryLotId, false,
                                         cancellationToken)
                                     ?? throw new NotFoundException(requestLine.InventoryItemId.ToString(),
                                         "Stock position");
            InventoryAccess.ApplyDomainAction(nameof(command.Lines), () => issue.AddLine(requestLine,
                position.Id, lot?.Id, lot?.Code, requestedLine.Quantity, issue.Version));
        }

        inventoryRepository.Add(issue);
        InventoryAudit.Issue(inventoryRepository, tenant, farm, user, issue, "DraftCreated",
            timeProvider.GetUtcNow(), null, "Partial stock issue draft created from an approved request.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return issue.Id;
    }
}
