namespace Cane360.Application.Inventory;

public sealed record CancelInputRequestCommand(
    Guid InputRequestId, long ExpectedVersion, string Reason) : IRequest;
