namespace Cane360.Application.Inventory;

public sealed record InventoryLossDto(
    Guid Id,
    Guid ActivityId,
    Guid StockIssueLineId,
    string ItemCode,
    string? LotCode,
    string UnitCode,
    decimal Quantity,
    string LossType,
    string Reason,
    string Status,
    long Version);
