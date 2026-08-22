namespace Cane360.Application.Inventory;

public sealed record StockMovementDto(
    Guid Id,
    long PostingSequence,
    Guid InventoryItemId,
    Guid? InventoryLotId,
    string ItemCode,
    string ItemName,
    string? LotCode,
    string UnitCode,
    string MovementType,
    decimal SignedQuantity,
    decimal SignedValueUsd,
    DateOnly EventDate,
    DateTimeOffset PostedAt,
    string PostedByUserId,
    Guid? OperationalPersonId,
    Guid StockReceiptLineId,
    Guid? ReversalOfStockMovementId);
