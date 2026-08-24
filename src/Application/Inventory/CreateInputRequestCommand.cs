namespace Cane360.Application.Inventory;

public sealed record CreateInputRequestCommand(
    Guid ActivityId,
    IReadOnlyList<CreateInputRequestLineCommand> Lines) : IRequest<Guid>;
