namespace Cane360.Application.Inventory;

public sealed record FieldReceiptLineDto(
    Guid Id,
    Guid StockIssueLineId,
    string ItemCode,
    string? LotCode,
    string UnitCode,
    decimal Quantity);
