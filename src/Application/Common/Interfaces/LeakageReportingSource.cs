using Cane360.Domain.Inventory;

namespace Cane360.Application.Common.Interfaces;

public sealed record LeakageReportingSource(IReadOnlyList<ControlException> Exceptions,
    IReadOnlyList<InputApplication> Applications, IReadOnlyList<InventoryLoss> Losses,
    IReadOnlyList<StockCount> Counts, IReadOnlyList<StockAdjustment> Adjustments,
    IReadOnlyList<StockIssue> Issues, IReadOnlyList<InputRequest> Requests,
    IReadOnlyList<FieldReceipt> FieldReceipts, IReadOnlyList<ApprovalDecision> Approvals,
    IReadOnlyList<StockMovement> Movements);
