namespace Cane360.Domain.Inventory;

public enum StockReceiptStatus
{
    Draft,
    PendingApproval,
    Approved,
    Posted,
    Reversed,
    Cancelled
}
