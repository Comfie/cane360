namespace Cane360.Application.Inventory;

public sealed record CreateStockReturnCommand(Guid ActivityId, DateOnly ReturnDate, Guid SenderPersonId, Guid ReceiverPersonId,
    IReadOnlyList<CreateStockReturnLineCommand> Lines) : IRequest<Guid>;
