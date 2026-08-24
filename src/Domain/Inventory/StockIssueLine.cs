namespace Cane360.Domain.Inventory;

public sealed class StockIssueLine : BaseEntity
{
    private StockIssueLine() { }

    private StockIssueLine(Guid tenantId, Guid farmId, Guid issueId, int lineNumber,
        InputRequestLine requestLine, Guid stockPositionId, Guid? inventoryLotId, string? lotCode,
        decimal quantity)
    {
        if (quantity <= 0) throw new InvalidOperationException("Issue quantity must be positive.");
        TenantId = tenantId;
        FarmId = farmId;
        StockIssueId = issueId;
        LineNumber = lineNumber;
        InputRequestLineId = requestLine.Id;
        InventoryItemId = requestLine.InventoryItemId;
        InventoryLotId = inventoryLotId;
        StockPositionId = stockPositionId;
        UnitOfMeasureId = requestLine.UnitOfMeasureId;
        ItemCodeSnapshot = requestLine.ItemCodeSnapshot;
        ItemNameSnapshot = requestLine.ItemNameSnapshot;
        LotCodeSnapshot = lotCode;
        UnitCodeSnapshot = requestLine.UnitCodeSnapshot;
        Quantity = Round(quantity);
    }

    public Guid TenantId { get; private set; }
    public Guid FarmId { get; private set; }
    public Guid StockIssueId { get; private set; }
    public int LineNumber { get; private set; }
    public Guid InputRequestLineId { get; private set; }
    public Guid InventoryItemId { get; private set; }
    public Guid? InventoryLotId { get; private set; }
    public Guid StockPositionId { get; private set; }
    public Guid UnitOfMeasureId { get; private set; }
    public string ItemCodeSnapshot { get; private set; } = string.Empty;
    public string ItemNameSnapshot { get; private set; } = string.Empty;
    public string? LotCodeSnapshot { get; private set; }
    public string UnitCodeSnapshot { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal? IssueUnitCostUsd { get; private set; }
    public decimal? IssueValueUsd { get; private set; }

    internal static StockIssueLine Create(Guid tenantId, Guid farmId, Guid issueId, int lineNumber,
        InputRequestLine requestLine, Guid stockPositionId, Guid? inventoryLotId, string? lotCode,
        decimal quantity) =>
        new(tenantId, farmId, issueId, lineNumber, requestLine, stockPositionId, inventoryLotId, lotCode, quantity);

    public void LockCost(decimal unitCostUsd)
    {
        if (IssueUnitCostUsd.HasValue) throw new InvalidOperationException("Issue cost has already been locked.");
        if (unitCostUsd < 0) throw new InvalidOperationException("Issue cost cannot be negative.");
        IssueUnitCostUsd = Round(unitCostUsd);
        IssueValueUsd = Round(Quantity * unitCostUsd);
    }

    private static decimal Round(decimal value) => decimal.Round(value, 6, MidpointRounding.AwayFromZero);
}
