using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed record DecideInventoryLossCommand(Guid InventoryLossId, long ExpectedVersion,
    ApprovalOutcome Outcome, string? Reason, string IdempotencyKey) : IRequest;
