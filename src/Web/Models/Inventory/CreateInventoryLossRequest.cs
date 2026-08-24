using Cane360.Domain.Inventory;

namespace Cane360.Web.Models.Inventory;

public sealed record CreateInventoryLossRequest(Guid ActivityId, Guid StockIssueLineId, decimal Quantity,
    InventoryLossType LossType, string Reason);
