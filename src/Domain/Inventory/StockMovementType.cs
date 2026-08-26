namespace Cane360.Domain.Inventory;

public enum StockMovementType
{
    PurchaseReceipt,
    OpeningBalance,
    ReceiptReversal,
    StockIssue,
    IssueReversal,
    StockReturn,
    ReturnReversal,
    StockAdjustment,
    AdjustmentReversal
}
