namespace Cane360.Application.Inventory;

public sealed record SubmitInventoryLossCommand(Guid InventoryLossId, long ExpectedVersion) : IRequest;
