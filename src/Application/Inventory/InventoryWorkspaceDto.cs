namespace Cane360.Application.Inventory;

public sealed record InventoryWorkspaceDto(
    string StoreCode,
    string StoreName,
    IReadOnlyList<UnitOfMeasureDto> Units,
    IReadOnlyList<InventoryItemDto> Items,
    IReadOnlyList<SupplierDto> Suppliers,
    IReadOnlyList<InventoryLotDto> Lots,
    IReadOnlyList<StockReceiptDto> Receipts,
    IReadOnlyList<StockOnHandDto> StockOnHand,
    IReadOnlyList<StockMovementDto> RecentMovements);
