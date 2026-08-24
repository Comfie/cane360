namespace Cane360.Application.Inventory;

public sealed record SubmitInputRequestCommand(Guid InputRequestId, long ExpectedVersion, string IdempotencyKey) : IRequest;
