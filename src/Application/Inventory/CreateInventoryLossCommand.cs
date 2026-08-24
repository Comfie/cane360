using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed record CreateInventoryLossCommand(Guid ActivityId, Guid StockIssueLineId, decimal Quantity,
    InventoryLossType LossType, string Reason) : IRequest<Guid>;
