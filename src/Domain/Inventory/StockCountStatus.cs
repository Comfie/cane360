namespace Cane360.Domain.Inventory;

public enum StockCountStatus
{
    Draft,
    InProgress,
    Review,
    PendingAdjustment,
    ClosedNoVariance,
    Closed,
    Cancelled
}
