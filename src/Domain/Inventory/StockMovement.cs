namespace Cane360.Domain.Inventory;

public sealed class StockMovement : BaseEntity
{
    private StockMovement() { }

    private StockMovement(
        Guid tenantId,
        Guid farmId,
        Guid storeId,
        Guid positionId,
        StockReceiptLine receiptLine,
        StockMovementType movementType,
        decimal signedQuantity,
        decimal signedValueUsd,
        DateOnly eventDate,
        DateTimeOffset postedAt,
        string postedByUserId,
        Guid? operationalPersonId,
        string postingIdentity,
        Guid? reversalOfStockMovementId)
    {
        TenantId = tenantId;
        FarmId = farmId;
        StoreId = storeId;
        StockPositionId = positionId;
        InventoryItemId = receiptLine.InventoryItemId;
        InventoryLotId = receiptLine.InventoryLotId;
        UnitOfMeasureId = receiptLine.UnitOfMeasureId;
        ItemCodeSnapshot = receiptLine.ItemCodeSnapshot;
        ItemNameSnapshot = receiptLine.ItemNameSnapshot;
        LotCodeSnapshot = receiptLine.LotCodeSnapshot;
        UnitCodeSnapshot = receiptLine.UnitCodeSnapshot;
        MovementType = movementType;
        SignedQuantity = decimal.Round(signedQuantity, 6, MidpointRounding.AwayFromZero);
        SignedValueUsd = decimal.Round(signedValueUsd, 6, MidpointRounding.AwayFromZero);
        EventDate = eventDate;
        PostedAt = postedAt;
        PostedByUserId = postedByUserId.Trim();
        OperationalPersonId = operationalPersonId;
        PostingIdentity = postingIdentity.Trim();
        StockReceiptLineId = receiptLine.Id;
        ReversalOfStockMovementId = reversalOfStockMovementId;
    }

    private StockMovement(
        StockIssue issue,
        StockIssueLine issueLine,
        StockMovementType movementType,
        decimal signedQuantity,
        decimal signedValueUsd,
        DateOnly eventDate,
        DateTimeOffset postedAt,
        string postedByUserId,
        string postingIdentity,
        Guid? reversalOfStockMovementId)
    {
        TenantId = issue.TenantId;
        FarmId = issue.FarmId;
        StoreId = issue.StoreId;
        StockPositionId = issueLine.StockPositionId;
        InventoryItemId = issueLine.InventoryItemId;
        InventoryLotId = issueLine.InventoryLotId;
        UnitOfMeasureId = issueLine.UnitOfMeasureId;
        ItemCodeSnapshot = issueLine.ItemCodeSnapshot;
        ItemNameSnapshot = issueLine.ItemNameSnapshot;
        LotCodeSnapshot = issueLine.LotCodeSnapshot;
        UnitCodeSnapshot = issueLine.UnitCodeSnapshot;
        MovementType = movementType;
        SignedQuantity = decimal.Round(signedQuantity, 6, MidpointRounding.AwayFromZero);
        SignedValueUsd = decimal.Round(signedValueUsd, 6, MidpointRounding.AwayFromZero);
        EventDate = eventDate;
        PostedAt = postedAt;
        PostedByUserId = postedByUserId.Trim();
        OperationalPersonId = issue.IssuerPersonId;
        PostingIdentity = postingIdentity.Trim();
        StockIssueLineId = issueLine.Id;
        ReversalOfStockMovementId = reversalOfStockMovementId;
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StoreId { get; private set; }
    public Guid StockPositionId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? InventoryLotId { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty;
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string? LotCodeSnapshot { get; private set; }
    public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public StockMovementType MovementType { get; private set; }
    public decimal SignedQuantity { get; private set; }
    public decimal SignedValueUsd { get; private set; }
    public DateOnly EventDate { get; private set; }
    public DateTimeOffset PostedAt { get; private set; }
    public string PostedByUserId { get; private set; } = string.Empty;
    public Guid? OperationalPersonId { get; private set; }
    public long PostingSequence { get; private set; }
    public string PostingIdentity { get; private set; } = string.Empty;
    public Guid? StockReceiptLineId { get; private set; }
    public Guid? StockIssueLineId { get; private set; }
    public Guid? ReversalOfStockMovementId { get; private set; }

    public static StockMovement CreateReceipt(
        Guid tenantId,
        Guid farmId,
        Guid storeId,
        Guid positionId,
        StockReceiptLine line,
        StockReceiptType receiptType,
        DateOnly eventDate,
        DateTimeOffset postedAt,
        string postedByUserId,
        Guid? operationalPersonId,
        string postingIdentity) =>
        new(
            tenantId,
            farmId,
            storeId,
            positionId,
            line,
            receiptType == StockReceiptType.Purchase
                ? StockMovementType.PurchaseReceipt
                : StockMovementType.OpeningBalance,
            line.Quantity,
            line.LineValueUsd,
            eventDate,
            postedAt,
            postedByUserId,
            operationalPersonId,
            postingIdentity,
            null);

    public static StockMovement CreateReversal(
        StockMovement original,
        StockReceiptLine line,
        DateOnly eventDate,
        DateTimeOffset postedAt,
        string postedByUserId,
        string postingIdentity) =>
        new(
            original.TenantId,
            original.FarmId,
            original.StoreId,
            original.StockPositionId,
            line,
            StockMovementType.ReceiptReversal,
            -original.SignedQuantity,
            -original.SignedValueUsd,
            eventDate,
            postedAt,
            postedByUserId,
            original.OperationalPersonId,
            postingIdentity,
            original.Id);

    public static StockMovement CreateIssue(
        StockIssue issue,
        StockIssueLine line,
        DateTimeOffset postedAt,
        string postedByUserId,
        string postingIdentity) =>
        new(issue, line, StockMovementType.StockIssue, -line.Quantity, -line.IssueValueUsd!.Value,
            issue.IssueDate, postedAt, postedByUserId, postingIdentity, null);

    public static StockMovement CreateIssueReversal(
        StockMovement original,
        StockIssue issue,
        StockIssueLine line,
        DateOnly eventDate,
        DateTimeOffset postedAt,
        string postedByUserId,
        string postingIdentity) =>
        new(issue, line, StockMovementType.IssueReversal, -original.SignedQuantity,
            -original.SignedValueUsd, eventDate, postedAt, postedByUserId, postingIdentity, original.Id);
}
