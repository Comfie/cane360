namespace Cane360.Domain.Inventory;

public sealed class StockAdjustment : BaseAuditableEntity
{
    private StockAdjustment() { }

    private StockAdjustment(Guid tenantId, Guid farmId, Guid storeId, StockPosition position, InventoryItem item,
        InventoryLot? lot, UnitOfMeasure unit, Guid? countLineId, StockAdjustmentType adjustmentType,
        decimal signedQuantity, decimal? explicitUnitValueUsd, long? countLineVersion, long? countVersion, string reason, DateOnly eventDate,
        string createdByUserId)
    {
        if (signedQuantity == 0) throw new InvalidOperationException("Adjustment quantity must be non-zero.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason); ArgumentException.ThrowIfNullOrWhiteSpace(createdByUserId);
        TenantId = tenantId; FarmId = farmId; StoreId = storeId; StockPositionId = position.Id; StockCountLineId = countLineId;
        InventoryItemId = item.Id; InventoryLotId = lot?.Id; UnitOfMeasureId = unit.Id;
        ItemCodeSnapshot = item.Code; ItemNameSnapshot = item.Name; LotCodeSnapshot = lot?.Code; UnitCodeSnapshot = unit.Code;
        AdjustmentType = adjustmentType; SignedQuantity = decimal.Round(signedQuantity, 6, MidpointRounding.AwayFromZero);
        ExplicitUnitValueUsd = explicitUnitValueUsd is null ? null : decimal.Round(explicitUnitValueUsd.Value, 6, MidpointRounding.AwayFromZero);
        SourceCountLineVersion = countLineVersion; SourceCountVersion = countVersion;
        Reason = reason.Trim(); EventDate = eventDate; CreatedByUserId = createdByUserId.Trim(); Status = StockAdjustmentStatus.Draft; Version = 1;
    }

    public Guid TenantId { get; private set; } public Guid FarmId { get; private set; } public Guid StoreId { get; private set; }
    public Guid StockPositionId { get; private set; } public Guid? StockCountLineId { get; private set; }
    public Guid InventoryItemId { get; private set; } public Guid? InventoryLotId { get; private set; } public Guid UnitOfMeasureId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty; public string ItemNameSnapshot { get; private set; } = string.Empty; public string? LotCodeSnapshot { get; private set; } public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public StockAdjustmentType AdjustmentType { get; private set; } public decimal SignedQuantity { get; private set; } public decimal? ExplicitUnitValueUsd { get; private set; }
    public long? SourceCountLineVersion { get; private set; } public long? SourceCountVersion { get; private set; }
    public decimal? UnitCostUsdSnapshot { get; private set; } public decimal? SignedValueUsdSnapshot { get; private set; }
    public string Reason { get; private set; } = string.Empty; public DateOnly EventDate { get; private set; } public string CreatedByUserId { get; private set; } = string.Empty;
    public StockAdjustmentStatus Status { get; private set; } public long Version { get; private set; }
    public DateTimeOffset? SubmittedAt { get; private set; } public DateTimeOffset? PostedAt { get; private set; } public Guid? StockMovementId { get; private set; }
    public Guid? ReversalOfStockAdjustmentId { get; private set; } public Guid? ReversalStockAdjustmentId { get; private set; } public string? CancellationReason { get; private set; }

    public static StockAdjustment Create(Guid tenantId, Guid farmId, Guid storeId, StockPosition position, InventoryItem item,
        InventoryLot? lot, UnitOfMeasure unit, Guid? countLineId, StockAdjustmentType adjustmentType, decimal signedQuantity,
        decimal? explicitUnitValueUsd, long? countLineVersion, long? countVersion, string reason, DateOnly eventDate, string createdByUserId) =>
        new(tenantId, farmId, storeId, position, item, lot, unit, countLineId, adjustmentType, signedQuantity, explicitUnitValueUsd, countLineVersion, countVersion, reason, eventDate, createdByUserId);

    public void Submit(DateTimeOffset submittedAt, long expectedVersion)
    {
        Require(expectedVersion); if (Status != StockAdjustmentStatus.Draft) throw new InvalidOperationException("Only a draft adjustment can be submitted.");
        SubmittedAt = submittedAt; Status = StockAdjustmentStatus.PendingGrowerApproval; Version++;
    }

    public void Decide(ApprovalOutcome outcome, long expectedVersion)
    {
        Require(expectedVersion); if (Status != StockAdjustmentStatus.PendingGrowerApproval) throw new InvalidOperationException("Only a pending adjustment can be decided.");
        Status = outcome == ApprovalOutcome.Approved ? StockAdjustmentStatus.Approved : StockAdjustmentStatus.Rejected; Version++;
    }

    public void Post(decimal unitCostUsd, DateTimeOffset postedAt, Guid movementId, long expectedVersion)
    {
        Require(expectedVersion); if (Status != StockAdjustmentStatus.Approved) throw new InvalidOperationException("Only an approved adjustment can post.");
        UnitCostUsdSnapshot = decimal.Round(unitCostUsd, 6, MidpointRounding.AwayFromZero);
        SignedValueUsdSnapshot = decimal.Round(SignedQuantity * UnitCostUsdSnapshot.Value, 6, MidpointRounding.AwayFromZero);
        PostedAt = postedAt; StockMovementId = movementId; Status = StockAdjustmentStatus.Posted; Version++;
    }

    public void MarkReversed(Guid reversalId)
    {
        if (Status != StockAdjustmentStatus.Posted) throw new InvalidOperationException("Only a posted adjustment can be reversed.");
        ReversalStockAdjustmentId = reversalId; Status = StockAdjustmentStatus.Reversed; Version++;
    }

    public void MarkReversalOf(Guid originalId)
    {
        ReversalOfStockAdjustmentId = originalId;
    }

    public void PostAuthorisedReversal(decimal unitCostUsd, DateTimeOffset postedAt, Guid movementId, Guid originalId)
    {
        if (Status != StockAdjustmentStatus.Draft) throw new InvalidOperationException("Only a new correction can become a reversal.");
        UnitCostUsdSnapshot = decimal.Round(unitCostUsd, 6, MidpointRounding.AwayFromZero);
        SignedValueUsdSnapshot = decimal.Round(SignedQuantity * UnitCostUsdSnapshot.Value, 6, MidpointRounding.AwayFromZero);
        PostedAt = postedAt; StockMovementId = movementId; ReversalOfStockAdjustmentId = originalId; Status = StockAdjustmentStatus.Posted; Version++;
    }

    public void Cancel(string reason, long expectedVersion)
    {
        Require(expectedVersion); if (Status != StockAdjustmentStatus.Draft) throw new InvalidOperationException("Only a draft adjustment can be cancelled.");
        ArgumentException.ThrowIfNullOrWhiteSpace(reason); CancellationReason = reason.Trim(); Status = StockAdjustmentStatus.Cancelled; Version++;
    }

    private void Require(long expectedVersion) { if (Version != expectedVersion) throw new InvalidOperationException("This adjustment changed after it was loaded. Refresh and try again."); }
}
