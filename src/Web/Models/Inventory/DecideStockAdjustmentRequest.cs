using Cane360.Domain.Inventory;

namespace Cane360.Web.Models.Inventory;

public sealed record DecideStockAdjustmentRequest(long ExpectedVersion, ApprovalOutcome Outcome, string? Reason, string IdempotencyKey);
