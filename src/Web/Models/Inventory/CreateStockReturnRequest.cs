namespace Cane360.Web.Models.Inventory;

public sealed record CreateStockReturnRequest(Guid ActivityId, DateOnly ReturnDate, Guid SenderPersonId,
    Guid ReceiverPersonId, IReadOnlyList<CreateStockReturnLineRequest> Lines);
