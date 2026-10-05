namespace Cane360.Application.Inventory;

public sealed class CreateInputRequestCommandHandler(
    IFarmSetupRepository farmRepository,
    IInventoryRepository inventoryRepository,
    IUser user,
    TimeProvider timeProvider) : IRequestHandler<CreateInputRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreateInputRequestCommand request, CancellationToken cancellationToken)
    {
        Tenant tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        Farm farm = InventoryAccess.RequireFarm(tenant);
        string userId = InventoryAccess.RequireUserId(user);

        (Field Field, CropCycle Cycle, Activity Activity) context =
            InventoryAccess.RequireOperationalActivity(farm, request.ActivityId);
        DateOnly operationalDate = InventoryAccess.OperationalDate(context.Activity);
        if (request.Lines.Count == 0)
        {
            throw InventoryAccess.Failure(nameof(request.Lines), "Add at least one input item.");
        }

        InputRequest inputRequest = InputRequest.Create(tenant.Id, farm.Id, context.Field.Id, context.Cycle.Id,
            context.Activity.Id, operationalDate, userId);
        foreach (CreateInputRequestLineCommand requestedLine in request.Lines)
        {
            InventoryItem item = await inventoryRepository.GetItemAsync(tenant.Id, farm.Id,
                                     requestedLine.InventoryItemId, false, cancellationToken)
                                 ?? throw new NotFoundException(requestedLine.InventoryItemId.ToString(),
                                     "Inventory item");
            InventoryApplicationRule rule = await inventoryRepository.GetEffectiveRuleAsync(tenant.Id, farm.Id, item.Id,
                                                context.Activity.ActivityTypeId, operationalDate, cancellationToken)
                                            ?? throw InventoryAccess.Failure(nameof(request.Lines),
                                                $"No effective application rule exists for {item.Code} and {context.Activity.ActivityTypeName} on {operationalDate:yyyy-MM-dd}.");
            decimal coverage = rule.CoverageBasis switch
            {
                ApplicationCoverageBasis.FieldReportingHectares => context.Field.ReportingHectares,
                ApplicationCoverageBasis.ActivityActualQuantity when context.Activity.ActualQuantity is > 0 => context
                    .Activity.ActualQuantity.Value,
                _ => throw InventoryAccess.Failure(nameof(request.ActivityId),
                    "The effective rule needs recorded activity quantity, but this activity has none.")
            };
            (decimal Quantity, decimal ValueUsd) stock =
                await inventoryRepository.GetItemStockSnapshotAsync(tenant.Id, farm.Id, item.Id, cancellationToken);
            decimal? average = stock.Quantity > 0 && stock.ValueUsd >= 0
                ? decimal.Round(stock.ValueUsd / stock.Quantity, 6, MidpointRounding.AwayFromZero)
                : null;
            inputRequest.AddLine(item, rule, coverage, requestedLine.RequestedQuantity,
                stock.Quantity, average, inputRequest.Version);
        }

        inventoryRepository.Add(inputRequest);
        InventoryAudit.Request(inventoryRepository, tenant, farm, user, inputRequest, "DraftCreated",
            timeProvider.GetUtcNow(), null, "Activity-linked input request draft created.");
        await inventoryRepository.SaveChangesAsync(cancellationToken);
        return inputRequest.Id;
    }
}
