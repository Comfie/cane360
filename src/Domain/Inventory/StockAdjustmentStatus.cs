namespace Cane360.Domain.Inventory;

public enum StockAdjustmentStatus
{
    Draft,
    PendingGrowerApproval,
    Approved,
    Rejected,
    Posted,
    Reversed,
    Cancelled
}
