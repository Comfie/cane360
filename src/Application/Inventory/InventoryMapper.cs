using Cane360.Application.Common.Exceptions;
using Cane360.Domain.Auditing;
using Cane360.Domain.Farms;
using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

internal static class InventoryMapper
{
    public static UnitOfMeasureDto Unit(UnitOfMeasure unit) => new(
        unit.Id, unit.Code, unit.Name, unit.Dimension, unit.DecimalPlaces, unit.Status.ToString(), unit.Version);

    public static InventoryItemDto Item(InventoryItem item) => new(
        item.Id, item.Code, item.Name, item.Category.ToString(), item.StockUnitId, item.StockUnitCode,
        item.ReorderLevel, item.LotTrackingPolicy.ToString(), item.ExpiryPolicy.ToString(),
        item.CostingMethod.ToString(), item.Status.ToString(), item.Version);

    public static SupplierDto Supplier(Supplier supplier) => new(
        supplier.Id, supplier.Code, supplier.Name, supplier.Contact, supplier.Status.ToString(), supplier.Version);

    public static InventoryLotDto Lot(InventoryLot lot) => new(
        lot.Id, lot.InventoryItemId, lot.Code, lot.ExpiryDate, lot.Status.ToString(), lot.Version);

    public static StockReceiptDto Receipt(Tenant tenant, Farm farm, StockReceipt receipt)
    {
        var personName = receipt.ReceivedByPersonId.HasValue
            ? farm.Persons.SingleOrDefault(person => person.Id == receipt.ReceivedByPersonId)?.DisplayName
            : null;
        return new StockReceiptDto(
            receipt.Id, receipt.ReceiptType.ToString(), receipt.SupplierId, null,
            receipt.ReceiptDate, receipt.ReceivedByPersonId, personName, receipt.SourceReference,
            receipt.Reason, receipt.LateEntryReason, receipt.Status.ToString(), receipt.PostedAt,
            receipt.ReversedAt, receipt.Version, receipt.Lines.Sum(line => line.LineValueUsd),
            receipt.Lines.OrderBy(line => line.LineNumber).Select(line => new StockReceiptLineDto(
                line.Id, line.LineNumber, line.InventoryItemId, line.InventoryLotId,
                line.ItemCodeSnapshot, line.ItemNameSnapshot, line.LotCodeSnapshot,
                line.ExpiryDateSnapshot, line.UnitCodeSnapshot, line.Quantity,
                line.UnitCostUsd, line.LineValueUsd)).ToArray());
    }

    public static StockMovementDto Movement(StockMovement movement) => new(
        movement.Id, movement.PostingSequence, movement.InventoryItemId, movement.InventoryLotId,
        movement.ItemCodeSnapshot, movement.ItemNameSnapshot, movement.LotCodeSnapshot,
        movement.UnitCodeSnapshot, movement.MovementType.ToString(), movement.SignedQuantity,
        movement.SignedValueUsd, movement.EventDate, movement.PostedAt, movement.PostedByUserId,
        movement.OperationalPersonId, movement.StockReceiptLineId, movement.StockIssueLineId,
        movement.ReversalOfStockMovementId);

    public static InventoryWorkspaceDto Workspace(
        Tenant tenant,
        Farm farm,
        IReadOnlyList<UnitOfMeasure> units,
        IReadOnlyList<InventoryItem> items,
        IReadOnlyList<Supplier> suppliers,
        IReadOnlyList<InventoryLot> lots,
        IReadOnlyList<StockReceipt> receipts,
        IReadOnlyList<(StockPosition Position, StockLedgerSnapshot Snapshot)> stock,
        IReadOnlyList<StockMovement> movements)
    {
        var itemMap = items.ToDictionary(item => item.Id);
        var lotMap = lots.ToDictionary(lot => lot.Id);
        return new InventoryWorkspaceDto(
            farm.Store.Code,
            farm.Store.Name,
            units.Select(Unit).ToArray(),
            items.Select(Item).ToArray(),
            suppliers.Select(Supplier).ToArray(),
            lots.Select(Lot).ToArray(),
            receipts.Select(receipt =>
            {
                var mapped = Receipt(tenant, farm, receipt);
                var supplierName = receipt.SupplierId.HasValue
                    ? suppliers.SingleOrDefault(supplier => supplier.Id == receipt.SupplierId)?.Name
                    : null;
                return mapped with { SupplierName = supplierName };
            }).ToArray(),
            stock.Select(pair =>
            {
                var item = itemMap[pair.Position.InventoryItemId];
                InventoryLot? lot = pair.Position.InventoryLotId.HasValue
                    ? lotMap.GetValueOrDefault(pair.Position.InventoryLotId.Value)
                    : null;
                return new StockOnHandDto(
                    pair.Position.Id, item.Id, lot?.Id, item.Code, item.Name, lot?.Code,
                    item.StockUnitCode, pair.Snapshot.Quantity, pair.Snapshot.ValueUsd,
                    pair.Snapshot.WeightedAverageUnitCostUsd, item.ReorderLevel);
            }).ToArray(),
            movements.OrderByDescending(movement => movement.PostingSequence).Take(100).Select(Movement).ToArray());
    }
}
