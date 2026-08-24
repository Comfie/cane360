using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

internal static class InventoryAccountability
{
    public static async Task<(decimal Received, decimal Applied, decimal Returned, decimal Loss, decimal Unaccounted)> GetAsync(
        IInventoryRepository repository, StockIssueLine line, CancellationToken cancellationToken)
    {
        var received = await repository.GetFieldReceivedQuantityAsync(line.Id, cancellationToken);
        var applied = await repository.GetConfirmedAppliedQuantityAsync(line.Id, cancellationToken);
        var returned = await repository.GetPostedReturnedQuantityAsync(line.Id, cancellationToken);
        var loss = await repository.GetApprovedLossQuantityAsync(line.Id, cancellationToken);
        return (received, applied, returned, loss, decimal.Round(line.Quantity - applied - returned - loss, 6));
    }

    public static async Task SynchronizeExceptionAsync(IInventoryRepository repository, Tenant tenant, Farm farm,
        IUser user, Guid activityId, StockIssueLine line, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var values = await GetAsync(repository, line, cancellationToken);
        var open = await repository.GetOpenControlExceptionAsync(tenant.Id, farm.Id, line.Id, cancellationToken);
        if (values.Unaccounted > 0 && open is null)
        {
            var created = ControlException.Open(tenant.Id, farm.Id, activityId, line.Id, line.Quantity, values.Applied, values.Returned, values.Loss, values.Unaccounted, now);
            repository.Add(created);
            InventoryAudit.Exception(repository, tenant, farm, user, created, "Opened", now,
                "Posted issue has non-zero unaccounted quantity and blocks activity closure.");
        }
        else if (values.Unaccounted == 0 && open is not null)
        {
            open.Resolve(values.Applied, values.Returned, values.Loss, now);
            InventoryAudit.Exception(repository, tenant, farm, user, open, "Resolved", now,
                "Confirmed application, posted return, or approved loss resolved the accountability discrepancy.");
        }
    }
}
