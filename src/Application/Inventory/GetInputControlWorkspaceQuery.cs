namespace Cane360.Application.Inventory;

public sealed record GetInputControlWorkspaceQuery(Guid? ActivityId) : IRequest<InputControlWorkspaceDto>;
