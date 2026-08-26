using Cane360.Domain.Auditing;

namespace Cane360.Application.Inventory;

public sealed record DecideStockAdjustmentCommand(Guid StockAdjustmentId, long ExpectedVersion, ApprovalOutcome Outcome,
    string? Reason, string IdempotencyKey) : IRequest<StockAdjustmentDto>;
