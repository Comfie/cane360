using Cane360.Domain.Inventory;

namespace Cane360.Application.Inventory;

public sealed record DecideInputRequestCommand(
    Guid InputRequestId, long ExpectedVersion, ApprovalOutcome Outcome,
    string? Reason, string IdempotencyKey) : IRequest;
