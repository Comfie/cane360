namespace Cane360.Application.Inventory;

public sealed record AttestInputApplicationCommand(Guid InputApplicationId, Guid SupervisorPersonId, string? Note, long ExpectedVersion) : IRequest;
