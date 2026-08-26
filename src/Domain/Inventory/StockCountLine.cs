namespace Cane360.Domain.Inventory;

public sealed class StockCountLine : BaseAuditableEntity
{
    private StockCountLine() { }

    private StockCountLine(StockCount count, StockPosition position, InventoryItem item, InventoryLot? lot,
        UnitOfMeasure unit, decimal expectedQuantity, decimal expectedValueUsd)
    {
        TenantId = count.TenantId; FarmId = count.FarmId; StockCountId = count.Id; StockPositionId = position.Id;
        InventoryItemId = item.Id; InventoryLotId = lot?.Id; UnitOfMeasureId = unit.Id;
        ItemCodeSnapshot = item.Code; ItemNameSnapshot = item.Name; LotCodeSnapshot = lot?.Code; UnitCodeSnapshot = unit.Code;
        ExpectedQuantity = decimal.Round(expectedQuantity, 6, MidpointRounding.AwayFromZero);
        ExpectedValueUsd = decimal.Round(expectedValueUsd, 6, MidpointRounding.AwayFromZero);
        Version = 1;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StockCountId { get; private set; }
    public Guid StockPositionId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? InventoryLotId { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty;
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string? LotCodeSnapshot { get; private set; }
    public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public decimal ExpectedQuantity { get; private set; }
    public decimal ExpectedValueUsd { get; private set; }
    public decimal? CountedQuantity { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset? EnteredAt { get; private set; }
    public string? EnteredByUserId { get; private set; }
    public long Version { get; private set; }
    public Guid? PostedStockAdjustmentId { get; private set; }
    public bool IsResolved => PostedStockAdjustmentId.HasValue;
    public decimal VarianceQuantity => (CountedQuantity ?? ExpectedQuantity) - ExpectedQuantity;

    public static StockCountLine Create(StockCount count, StockPosition position, InventoryItem item, InventoryLot? lot,
        UnitOfMeasure unit, decimal expectedQuantity, decimal expectedValueUsd) =>
        new(count, position, item, lot, unit, expectedQuantity, expectedValueUsd);

    public void Enter(decimal countedQuantity, string? notes, DateTimeOffset enteredAt, string enteredByUserId,
        long expectedVersion)
    {
        if (Version != expectedVersion) throw new InvalidOperationException("This count line changed after it was loaded. Refresh and try again.");
        if (countedQuantity < 0) throw new InvalidOperationException("Counted quantity cannot be negative.");
        ArgumentException.ThrowIfNullOrWhiteSpace(enteredByUserId);
        CountedQuantity = decimal.Round(countedQuantity, 6, MidpointRounding.AwayFromZero);
        Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        EnteredAt = enteredAt; EnteredByUserId = enteredByUserId.Trim(); Version++;
    }

    public void Resolve(Guid adjustmentId)
    {
        if (VarianceQuantity == 0) throw new InvalidOperationException("A zero variance cannot be adjusted.");
        if (PostedStockAdjustmentId.HasValue) throw new InvalidOperationException("This variance already has a posted adjustment.");
        PostedStockAdjustmentId = adjustmentId; Version++;
    }

    public void Reopen(Guid adjustmentId)
    {
        if (PostedStockAdjustmentId != adjustmentId) throw new InvalidOperationException("Only the posted adjustment can reopen this variance.");
        PostedStockAdjustmentId = null; Version++;
    }
}
