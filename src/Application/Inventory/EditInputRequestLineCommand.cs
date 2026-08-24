namespace Cane360.Application.Inventory;

public sealed record EditInputRequestLineCommand(
    Guid InputRequestId, Guid InputRequestLineId, decimal RequestedQuantity,
    long ExpectedVersion) : IRequest;
