namespace Cane360.Application.Inventory;

public sealed record ConfirmInputApplicationCommand(Guid InputApplicationId, string? LateConfirmationReason, long ExpectedVersion, string IdempotencyKey) : IRequest;
