namespace Cane360.Application.Inventory;

public sealed record StockIssueLineDto(
    Guid Id, Guid InputRequestLineId, Guid InventoryItemId, Guid? InventoryLotId,
    string ItemCode, string ItemName, string? LotCode, string UnitCode,
    decimal Quantity, decimal? IssueUnitCostUsd, decimal? IssueValueUsd);
