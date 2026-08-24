namespace Cane360.Application.Inventory;

public sealed record RevokeManagerInvitationCommand(Guid InvitationId, long ExpectedVersion) : IRequest;
