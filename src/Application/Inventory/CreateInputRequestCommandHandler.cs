using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed class CreateInputRequestCommandHandler(
    IFarmSetupRepository farmRepository, IInventoryRepository inventoryRepository,
    IUser user, TimeProvider timeProvider) : IRequestHandler<CreateInputRequestCommand, Guid>
{
    public async Task<Guid> Handle(CreateInputRequestCommand request, CancellationToken cancellationToken)
    {
        var tenant = await InventoryAccess.RequireTenantAsync(farmRepository, user, false, cancellationToken);
        var farm = InventoryAccess.RequireFarm(tenant);
        var userId = InventoryAccess.RequireUserId(user);
        InventoryAccess.RequireGrowerOrManager(tenant, userId);
        var context = InventoryAccess.RequireOperationalActivity(farm, request.ActivityId);
        var operationalDate = InventoryAccess.OperationalDate(context.Activity);
        if (request.Lines.Count == 0) throw InventoryAccess.Failure(nameof(request.Lines), "Add at least one input item.");
        var inputRequest = InputRequest.Create(tenant.Id, farm.Id, context.Field.Id, context.Cycle.Id,
            context.Activity.Id, operationalDate, userId);
        foreach (var requestedLine in request.Lines)
        {
            var item = await inventoryRepository.GetItemAsync(tenant.Id, farm.Id,
                requestedLine.InventoryItemId, false, cancellationToken)
                ?? throw new NotFoundException(requestedLine.InventoryItemId.ToString(), "Inventory item");
            var rule = await inventoryRepository.GetEffectiveRuleAsync(tenant.Id, farm.Id, item.Id,
                context.Activity.ActivityTypeId, operationalDate, cancellationToken)
                ?? throw InventoryAccess.Failure(nameof(request.Lines),
                    $"No effective application rule exists for {item.Code} and {context.Activity.ActivityTypeName} on {operationalDate:yyyy-MM-dd}.");
            var coverage = rule.CoverageBasis switch
            {
                ApplicationCoverageBasis.FieldReportingHectares => context.Field.ReportingHectares,
                ApplicationCoverageBasis.ActivityActualQuantity when context.Activity.ActualQuantity is > 0 => context.Activity.ActualQuantity.Value,
                _ => throw InventoryAccess.Failure(nameof(request.ActivityId),
                    "The effective rule needs recorded activity quantity, but this activity has none.")
            };
            var stock = await inventoryRepository.GetItemStockSnapshotAsync(tenant.Id, farm.Id, item.Id, cancellationToken);
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
